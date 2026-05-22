using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TripGeniusBackend.Application.Interfaces;
using TripGeniusBackend.Application.Interfaces.Repositories;
using TripGeniusBackend.Application.Interfaces.Services;
using TripGeniusBackend.Application.Moderation;
using TripGeniusBackend.Domain.Entities;
using TripGeniusBackend.Domain.Enums;

namespace TripGeniusBackend.Infrastructure.Services;

public class BackgroundModerationService : IBackgroundModerationService
{
    private const string RemovedPlaceholder = "[Removed]";
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackgroundModerationService> _logger;

    public BackgroundModerationService(
        IServiceScopeFactory scopeFactory,
        ILogger<BackgroundModerationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public void ScheduleImageReview(
        ModerationTarget target,
        int userId,
        int entityId,
        byte[] imageBytes,
        string? contentType)
    {
        if (imageBytes.Length == 0)
            return;

        var bytes = imageBytes;
        var type = contentType;
        _ = Task.Run(async () =>
        {
            try
            {
                await RunImageReviewAsync(target, userId, entityId, bytes, type);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background image moderation failed for {Target} {EntityId}", target, entityId);
            }
        });
    }

    public void ScheduleTextReview(
        ModerationTarget target,
        int userId,
        int entityId,
        IReadOnlyList<(string Field, string Value)> fields,
        int? relatedEntityId = null)
    {
        var snapshot = fields
            .Where(f => !string.IsNullOrWhiteSpace(f.Value))
            .Select(f => (f.Field, f.Value.Trim()))
            .ToList();

        if (snapshot.Count == 0)
            return;

        var related = relatedEntityId;
        _ = Task.Run(async () =>
        {
            try
            {
                await RunTextReviewAsync(target, userId, entityId, related, snapshot);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background text moderation failed for {Target} {EntityId}", target, entityId);
            }
        });
    }

    private async Task RunImageReviewAsync(
        ModerationTarget target,
        int userId,
        int entityId,
        byte[] imageBytes,
        string? contentType)
    {
        using var scope = _scopeFactory.CreateScope();
        var moderation = scope.ServiceProvider.GetRequiredService<IContentModerationService>();

        await using var stream = new MemoryStream(imageBytes);
        var result = await moderation.CheckImageAsync(stream, contentType);
        if (!result.IsBlocked)
            return;

        await ApplyImageSanctionAsync(scope, target, entityId);
        var (title, body, url, inApp) = NotificationCopy.ForImage(target);
        await NotifyUserAsync(scope, userId, title, body, url, inApp);
    }

    private async Task RunTextReviewAsync(
        ModerationTarget target,
        int userId,
        int entityId,
        int? relatedEntityId,
        IReadOnlyList<(string Field, string Value)> fields)
    {
        using var scope = _scopeFactory.CreateScope();
        var moderation = scope.ServiceProvider.GetRequiredService<IContentModerationService>();

        var blocked = new List<string>();
        foreach (var (field, value) in fields)
        {
            var result = await moderation.CheckTextAsync(value);
            if (result.IsBlocked)
                blocked.Add(field);
        }

        if (blocked.Count == 0)
            return;

        await ApplyTextSanctionsAsync(scope, target, entityId, relatedEntityId, blocked);
        var (title, body, url, inApp) = NotificationCopy.ForText(target);
        await NotifyUserAsync(scope, userId, title, body, url, inApp);
    }

    private static async Task ApplyImageSanctionAsync(
        IServiceScope scope,
        ModerationTarget target,
        int entityId)
    {
        switch (target)
        {
            case ModerationTarget.TripCover:
                await DeleteRegularTripAsync(scope, entityId);
                break;
            case ModerationTarget.OffroadTripCover:
                await DeleteOffroadTripAsync(scope, entityId);
                break;
            case ModerationTarget.UserAvatar:
            {
                var repo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                var user = await repo.GetUserById(entityId);
                if (user is null) return;
                user.UpdateProfile(user.Profile.Username, string.Empty, user.Profile.Description);
                await repo.SaveChanges();
                break;
            }
        }
    }

    private static async Task ApplyTextSanctionsAsync(
        IServiceScope scope,
        ModerationTarget target,
        int entityId,
        int? relatedEntityId,
        IReadOnlyList<string> blockedFields)
    {
        switch (target)
        {
            case ModerationTarget.TripDetails:
            case ModerationTarget.TripTimeline:
                await DeleteRegularTripAsync(scope, entityId);
                break;
            case ModerationTarget.OffroadTripDetails:
                await DeleteOffroadTripAsync(scope, entityId);
                break;
            case ModerationTarget.OffroadRoute:
                if (!relatedEntityId.HasValue) return;
                await DeleteOffroadRouteAsync(scope, relatedEntityId.Value, entityId);
                break;
            case ModerationTarget.UserProfile:
                await SanitizeUserProfileAsync(scope, entityId, blockedFields);
                break;
        }
    }

    private static async Task DeleteRegularTripAsync(IServiceScope scope, int tripId)
    {
        var repo = scope.ServiceProvider.GetRequiredService<ITripRepository>();
        await repo.DeleteTrip(tripId);
    }

    private static async Task DeleteOffroadTripAsync(IServiceScope scope, int tripId)
    {
        var repo = scope.ServiceProvider.GetRequiredService<IOffroadTripRepository>();
        await repo.DeleteTrip(tripId);
    }

    private static async Task DeleteOffroadRouteAsync(IServiceScope scope, int tripId, int routeId)
    {
        var repo = scope.ServiceProvider.GetRequiredService<IOffroadTripRepository>();
        var trip = await repo.GetTripById(tripId);
        if (trip is null) return;
        trip.RemoveRoute(routeId);
        await repo.SaveChanges();
    }

    private static async Task SanitizeUserProfileAsync(
        IServiceScope scope,
        int userId,
        IReadOnlyList<string> blockedFields)
    {
        var repo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var user = await repo.GetUserById(userId);
        if (user is null) return;

        var username = user.Profile.Username;
        var description = user.Profile.Description;
        var tags = user.Preferences.Tags.ToList();

        foreach (var field in blockedFields)
        {
            if (field == "username") username = RemovedPlaceholder;
            else if (field == "description") description = RemovedPlaceholder;
            else if (field.StartsWith("tags[", StringComparison.Ordinal))
            {
                var m = Regex.Match(field, @"tags\[(\d+)\]");
                if (m.Success && int.TryParse(m.Groups[1].Value, out var idx) && idx >= 0 && idx < tags.Count)
                    tags[idx] = RemovedPlaceholder;
            }
        }

        user.UpdateProfile(username, user.Profile.ProfileURL, description);
        user.UpdatePreferences(user.Preferences.MaxGroupSize, tags);
        await repo.SaveChanges();
    }

    private static async Task NotifyUserAsync(
        IServiceScope scope,
        int userId,
        string pushTitle,
        string pushBody,
        string url,
        string inAppMessage)
    {
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var push = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var user = await userRepo.GetUserById(userId);
        if (user is null) return;

        user.AddNotification(inAppMessage);
        await userRepo.SaveChanges();
        await push.SendNotificationAsync(userId, pushTitle, pushBody, url);
    }

    private static class NotificationCopy
    {
        public static (string Title, string Body, string Url, string InApp) ForImage(ModerationTarget target) =>
            target switch
            {
                ModerationTarget.TripCover => (
                    "Trip removed",
                    "Your trip was flagged as inappropriate and was deleted.",
                    "/app",
                    "Your trip was flagged as inappropriate and was deleted."),
                ModerationTarget.OffroadTripCover => (
                    "Offroad trip removed",
                    "Your offroad trip was flagged as inappropriate and was deleted.",
                    "/app/offroad",
                    "Your offroad trip was flagged as inappropriate and was deleted."),
                ModerationTarget.UserAvatar => (
                    "Profile flagged",
                    "Your profile picture was flagged as inappropriate and was removed.",
                    "/app/profile",
                    "Your profile picture was flagged as inappropriate and was removed."),
                _ => (
                    "Content flagged",
                    "Uploaded content was flagged as inappropriate and was removed.",
                    "/app",
                    "Uploaded content was flagged as inappropriate and was removed."),
            };

        public static (string Title, string Body, string Url, string InApp) ForText(ModerationTarget target) =>
            target switch
            {
                ModerationTarget.TripDetails => (
                    "Trip removed",
                    "Your trip was flagged as inappropriate and was deleted.",
                    "/app",
                    "Your trip was flagged as inappropriate and was deleted."),
                ModerationTarget.TripTimeline => (
                    "Trip removed",
                    "Your trip was flagged as inappropriate and was deleted.",
                    "/app",
                    "Your trip was flagged as inappropriate and was deleted."),
                ModerationTarget.OffroadTripDetails => (
                    "Offroad trip removed",
                    "Your offroad trip was flagged as inappropriate and was deleted.",
                    "/app/offroad",
                    "Your offroad trip was flagged as inappropriate and was deleted."),
                ModerationTarget.OffroadRoute => (
                    "Route removed",
                    "Your route was flagged as inappropriate and was deleted.",
                    "/app/offroad",
                    "Your route was flagged as inappropriate and was deleted."),
                ModerationTarget.UserProfile => (
                    "Profile flagged",
                    "Your profile was flagged as inappropriate. Some text was removed.",
                    "/app/profile",
                    "Your profile was flagged as inappropriate. Some text was removed."),
                _ => (
                    "Content flagged",
                    "Some content was flagged as inappropriate and was removed.",
                    "/app",
                    "Some content was flagged as inappropriate and was removed."),
            };
    }
}
