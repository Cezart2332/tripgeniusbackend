using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TripGeniusBackend.Application.DTOs.Trip;
using TripGeniusBackend.Application.Interfaces;
using TripGeniusBackend.Application.Interfaces.Repositories;
using TripGeniusBackend.Application.Interfaces.Services;
using TripGeniusBackend.Domain.Entities;
using TripGeniusBackend.Domain.Enums;

namespace TripGeniusBackend.Infrastructure.Persistence.Hubs;

[Authorize]
public class TripChatHub : Hub
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<TripChatHub> _hubContext;

    public TripChatHub(IServiceScopeFactory scopeFactory, IHubContext<TripChatHub> hubContext)
    {
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
    }

    private int GetUserId()
    {
        var value = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(value, out var userId))
            throw new HubException("Unauthorized");
        return userId;
    }

    private static async Task EnsureMemberAsync(ITripRepository tripRepository, int tripId, int userId)
    {
        var trip = await tripRepository.GetTripById(tripId);
        if (trip == null)
            throw new HubException("Trip not found");
        if (!trip.Members.Any(m => m.UserId == userId && m.MemberStatus == MemberStatus.Accepted))
            throw new HubException("You are not a member of this trip");
    }

    public async Task JoinTrip(int tripId)
    {
        using var scope = _scopeFactory.CreateScope();
        var tripRepository = scope.ServiceProvider.GetRequiredService<ITripRepository>();
        await EnsureMemberAsync(tripRepository, tripId, GetUserId());
        await Groups.AddToGroupAsync(Context.ConnectionId, $"trip-{tripId}");
    }

    public async Task LeaveTrip(int tripId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"trip-{tripId}");
    }

    public async Task<MessageResponse> SendMessage(int tripId, string content, int[]? mentionedUserIds = null)
    {
        using var scope = _scopeFactory.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var messageRepository = scope.ServiceProvider.GetRequiredService<IMessageRepository>();
        var tripRepository = scope.ServiceProvider.GetRequiredService<ITripRepository>();

        var userId = GetUserId();
        var trip = await tripRepository.GetTripById(tripId);
        if (trip == null)
            throw new HubException("Trip not found");
        if (!trip.Members.Any(m => m.UserId == userId && m.MemberStatus == MemberStatus.Accepted))
            throw new HubException("You are not a member of this trip");

        var user = await userRepository.GetUserById(userId);
        if (user == null)
            throw new KeyNotFoundException("User not found");

        var message = Message.Create(content, "", DateTime.UtcNow, userId, tripId);

        await messageRepository.AddMessage(message);
        await messageRepository.SaveChanges();

        var messageResponse = new MessageResponse
        {
            Id = message.Id,
            Content = message.Content,
            SentAt = message.Date,
            ImageUrl = message.ImageURL,
            Username = user.Profile.Username,
            ProfileUrl = user.Profile.ProfileURL,
            IsAi = false
        };

        var groupName = $"trip-{tripId}";
        await Clients.Group(groupName).SendAsync("ReceiveMessage", messageResponse);

        // Notify tagged members (verified against accepted membership, never the sender).
        NotifyMentionedMembers(trip, userId, user.Profile.Username, content, mentionedUserIds,
            $"/app/trip/{tripId}");

        // "@ai" → run the trip agent in the background and stream its reply to the group.
        if (MentionsAi(content))
            TriggerTripAgent(tripId, userId, StripAiMention(content));

        ChatModerationRunner.Schedule(
            _scopeFactory,
            _hubContext,
            groupName,
            message.Id,
            userId,
            content,
            async (services, messageId) =>
            {
                var repo = services.GetRequiredService<IMessageRepository>();
                var deleted = await repo.DeleteMessageAsync(messageId);
                if (deleted)
                    await repo.SaveChanges();
                return deleted;
            });

        // Returnat clientului care a trimis, ca să reconcilieze bula optimistă
        // (folosit la trimiterea offline: timestamp-ul real e cel de acum, la sincronizare).
        return messageResponse;
    }

    private static readonly Regex AiMentionRegex = new(@"(^|\s)@ai\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    internal static bool MentionsAi(string content) => AiMentionRegex.IsMatch(content);

    internal static string StripAiMention(string content) => AiMentionRegex.Replace(content, " ").Trim();

    private void TriggerTripAgent(int tripId, int userId, string prompt)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var ai = scope.ServiceProvider.GetRequiredService<ITripChatAiService>();
                await ai.RespondInTripAsync(tripId, userId, prompt, CancellationToken.None);
            }
            catch (Exception ex)
            {
                var logger = _scopeFactory.CreateScope().ServiceProvider.GetService<ILogger<TripChatHub>>();
                logger?.LogWarning(ex, "Trip agent trigger failed for trip {TripId}", tripId);
            }
        });
    }

    private void NotifyMentionedMembers(Trip trip, int senderId, string senderName, string content,
        int[]? mentionedUserIds, string url)
    {
        if (mentionedUserIds == null || mentionedUserIds.Length == 0) return;

        var targets = mentionedUserIds
            .Distinct()
            .Where(id => id != senderId && trip.Members.Any(m => m.UserId == id && m.MemberStatus == MemberStatus.Accepted))
            .ToList();
        if (targets.Count == 0) return;

        var preview = content.Length > 120 ? content[..120] + "…" : content;

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();

                foreach (var targetId in targets)
                {
                    var target = await userRepo.GetUserById(targetId);
                    if (target == null) continue;
                    target.AddNotification($"{senderName} mentioned you in \"{trip.Title}\".");
                    await userRepo.SaveChanges();
                    await notifications.SendNotificationAsync(targetId, $"{senderName} mentioned you", preview, url);
                }
            }
            catch
            {
                // Best-effort: a failed mention notification must not break chat.
            }
        });
    }
}
