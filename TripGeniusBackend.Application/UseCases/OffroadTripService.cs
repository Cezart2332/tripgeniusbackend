using Microsoft.Extensions.DependencyInjection;
using Pgvector;
using TripGeniusBackend.Application.DTOs.OffroadTrip;
using TripGeniusBackend.Application.DTOs.Trip;
using TripGeniusBackend.Application.Exceptions;
using TripGeniusBackend.Application.Helpers;
using TripGeniusBackend.Application.Interfaces;
using TripGeniusBackend.Application.Interfaces.Queries;
using TripGeniusBackend.Application.Interfaces.Repositories;
using TripGeniusBackend.Application.Interfaces.Services;
using TripGeniusBackend.Application.Interfaces.UseCases;
using TripGeniusBackend.Application.Moderation;
using TripGeniusBackend.Domain.Entities;
using TripGeniusBackend.Domain.Enums;

namespace TripGeniusBackend.Application.UseCases;

public class OffroadTripService : IOffroadTripService
{
    private readonly IOffroadTripRepository _tripRepository;
    private readonly IOffroadTripQueryService _tripQueryService;
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly IFileUploader _fileUploader;
    private readonly IOffroadMessageQueryService _messageQueryService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly INotificationService _notificationService;
    private readonly IGpxService _gpxService;
    private readonly IBackgroundModerationService _backgroundModeration;

    public OffroadTripService(
        IOffroadTripRepository tripRepository,
        IOffroadTripQueryService tripQueryService,
        IUserRepository userRepository,
        IJwtService jwtService,
        IFileUploader fileUploader,
        IOffroadMessageQueryService messageQueryService,
        IServiceScopeFactory scopeFactory,
        INotificationService notificationService,
        IGpxService gpxService,
        IBackgroundModerationService backgroundModeration)
    {
        _tripRepository = tripRepository;
        _tripQueryService = tripQueryService;
        _userRepository = userRepository;
        _jwtService = jwtService;
        _fileUploader = fileUploader;
        _messageQueryService = messageQueryService;
        _scopeFactory = scopeFactory;
        _notificationService = notificationService;
        _gpxService = gpxService;
        _backgroundModeration = backgroundModeration;
    }

    public async Task<int> CreateTrip(OffroadTripRequest request)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        var userId = _jwtService.GetUserId();
        var trip = OffroadTrip.Create(request.Title, request.Description, request.StartingDate,
            request.EndingDate, request.Tags, request.MaxParticipants, request.Price, userId);

        foreach (var route in request.Routes)
        {
            trip.AddRoute(route.StartDay, route.EndDay, route.Name, route.Note,
                OffroadRouteGeoJson.NormalizeForStorage(route.TrackGeoJson),
                route.Source, route.DistanceMeters, route.ElevationGainMeters);
        }

        var imageBytes = await StreamBuffer.ReadAllBytesAsync(request.ImageStream);

        await _tripRepository.CreateTrip(trip);
        await _tripRepository.SaveChanges();

        if (request.ImageStream != null)
        {
            var url = await _fileUploader.UploadFile(request.ImageStream,
                Path.GetExtension(request.ImageFileName), "offroad-trip", trip.Id);
            trip.SetImageUrl(url);
        }
        else if (!string.IsNullOrWhiteSpace(request.ImageUrl))
        {
            trip.SetImageUrl(request.ImageUrl);
        }
        await _tripRepository.SaveChanges();

        _backgroundModeration.ScheduleTextReview(
            ModerationTarget.OffroadTripDetails,
            userId,
            trip.Id,
            ModerationFields.ToReviewList(ModerationFields.FromOffroadTripRequest(request)));
        if (imageBytes is { Length: > 0 })
            _backgroundModeration.ScheduleImageReview(
                ModerationTarget.OffroadTripCover, userId, trip.Id, imageBytes, ImageContentType(request.ImageFileName));

        ScheduleEmbeddingUpdate(trip.Id);
        return trip.Id;
    }

    public async Task<List<OffroadTripResponse>> GetTripsForUser(OffroadTripsRequest request)
    {
        var userId = _jwtService.GetUserId();
        return await _tripQueryService.GetTripsForUser(userId, request);
    }

    public async Task<List<OffroadTripResponse>> GetTrips()
    {
        var userId = _jwtService.GetUserId();
        return await _tripQueryService.GetTrips(userId);
    }

    public async Task<OffroadTripResponse> GetTrip(int id)
    {
        var trip = await _tripQueryService.GetTrip(id, _jwtService.GetUserId());
        if (trip == null) throw new KeyNotFoundException("Offroad trip not found");
        return trip;
    }

    public async Task<OffroadTripResponse> UpdateTrip(UpdateOffroadTripRequest request)
    {
        var userId = _jwtService.GetUserId();
        var trip = await _tripRepository.GetTripById(request.Id);
        if (trip == null) throw new KeyNotFoundException("Offroad trip not found");
        if (!trip.Members.Any(m => m.UserId == userId && m.Role == Roles.Owner))
            throw new UnauthorizedAccessException("You are not authorized to do this");

        var imageBytes = await StreamBuffer.ReadAllBytesAsync(request.ImageStream);

        trip.UpdateTrip(request.Title, request.Description, request.StartingDate, request.EndingDate,
            request.Status, request.Tags, request.MaxParticipants, request.Price);

        if (request.ImageStream != null)
        {
            var url = await _fileUploader.UploadFile(request.ImageStream,
                Path.GetExtension(request.ImageFileName), "offroad-trip", trip.Id);
            trip.SetImageUrl(url);
        }

        await _tripRepository.SaveChanges();

        _backgroundModeration.ScheduleTextReview(
            ModerationTarget.OffroadTripDetails,
            userId,
            trip.Id,
            ModerationFields.ToReviewList(ModerationFields.FromOffroadTripUpdate(request)));
        if (imageBytes is { Length: > 0 })
            _backgroundModeration.ScheduleImageReview(
                ModerationTarget.OffroadTripCover, userId, trip.Id, imageBytes, ImageContentType(request.ImageFileName));

        ScheduleEmbeddingUpdate(trip.Id);
        return (await _tripQueryService.GetTrip(trip.Id, userId))!;
    }

    public async Task<OffroadRouteResponse> AddRoute(UpdateOffroadRouteRequest request, Stream? gpxStream = null)
    {
        var userId = _jwtService.GetUserId();
        var trip = await RequireOwnerTrip(request.TripId, userId);

        var (trackGeoJson, source, distance, elevation, originalGpx) =
            await ResolveRouteGeometry(request, gpxStream);

        trip.AddRoute(request.StartDay, request.EndDay, request.Name, request.Note, trackGeoJson,
            source, distance, elevation, originalGpx);
        await _tripRepository.SaveChanges();
        ScheduleEmbeddingUpdate(trip.Id);

        var route = trip.Routes.Last();
        _backgroundModeration.ScheduleTextReview(
            ModerationTarget.OffroadRoute,
            userId,
            route.Id,
            ModerationFields.ToReviewList(ModerationFields.FromOffroadRoute(request)),
            request.TripId);

        return (await _tripQueryService.GetRoute(route.Id))!;
    }

    public async Task<OffroadRouteResponse> UpdateRoute(UpdateOffroadRouteRequest request, Stream? gpxStream = null)
    {
        var userId = _jwtService.GetUserId();
        var trip = await RequireOwnerTrip(request.TripId, userId);

        var (trackGeoJson, source, distance, elevation, originalGpx) =
            await ResolveRouteGeometry(request, gpxStream);

        trip.UpdateRoute(request.Id, request.StartDay, request.EndDay, request.Name, request.Note,
            trackGeoJson, source, distance, elevation, originalGpx);
        await _tripRepository.SaveChanges();
        ScheduleEmbeddingUpdate(trip.Id);

        _backgroundModeration.ScheduleTextReview(
            ModerationTarget.OffroadRoute,
            userId,
            request.Id,
            ModerationFields.ToReviewList(ModerationFields.FromOffroadRoute(request)),
            request.TripId);

        return (await _tripQueryService.GetRoute(request.Id))!;
    }

    public async Task RemoveRoute(int tripId, int routeId)
    {
        var userId = _jwtService.GetUserId();
        var trip = await RequireOwnerTrip(tripId, userId);
        trip.RemoveRoute(routeId);
        await _tripRepository.SaveChanges();
        ScheduleEmbeddingUpdate(trip.Id);
    }

    public async Task<OffroadRouteResponse> GetRoute(int tripId, int routeId)
    {
        var userId = _jwtService.GetUserId();
        await RequireOwnerTrip(tripId, userId);
        var route = await _tripQueryService.GetRoute(routeId);
        if (route == null) throw new KeyNotFoundException("Route not found");
        return route;
    }

    public async Task<byte[]> ExportRouteGpx(int tripId, int routeId)
    {
        var trip = await _tripRepository.GetTripById(tripId);
        if (trip == null) throw new KeyNotFoundException("Offroad trip not found");
        var route = trip.Routes.FirstOrDefault(r => r.Id == routeId);
        if (route == null) throw new KeyNotFoundException("Route not found");
        return _gpxService.BuildRouteGpx(route, trip.Title);
    }

    public async Task<byte[]> ExportTripGpx(int tripId)
    {
        var trip = await _tripRepository.GetTripById(tripId);
        if (trip == null) throw new KeyNotFoundException("Offroad trip not found");
        return _gpxService.BuildTripGpx(trip);
    }

    public async Task MembershipRequest(int tripId, int invitedId) =>
        await HandleMembershipRequest(tripId, invitedId);

    public async Task MembershipResponse(int tripId, int invitedId, string status, string action) =>
        await HandleMembershipResponse(tripId, invitedId, status, action);

    public async Task RemoveMember(int tripId, int removedId) =>
        await HandleRemoveMember(tripId, removedId);

    public async Task UpdateMember(UpdateRoleRequest updateRoleRequest) =>
        await HandleUpdateMember(updateRoleRequest);

    public async Task<List<MessageResponse>> GetMessages(int tripId) =>
        await _messageQueryService.GetMessages(tripId);

    private async Task<(string trackGeoJson, RouteSource source, double distance, double elevation, string? originalGpx)>
        ResolveRouteGeometry(UpdateOffroadRouteRequest request, Stream? gpxStream)
    {
        if (gpxStream != null)
        {
            var parsed = await _gpxService.ParseGpxAsync(gpxStream);
            return (parsed.TrackGeoJson, RouteSource.Imported, parsed.DistanceMeters,
                parsed.ElevationGainMeters, parsed.OriginalGpx);
        }

        return (OffroadRouteGeoJson.NormalizeForStorage(request.TrackGeoJson), request.Source,
            request.DistanceMeters, request.ElevationGainMeters, null);
    }

    private async Task<OffroadTrip> RequireOwnerTrip(int tripId, int userId)
    {
        var trip = await _tripRepository.GetTripById(tripId);
        if (trip == null) throw new KeyNotFoundException("Offroad trip not found");
        if (!trip.Members.Any(m => m.UserId == userId && m.Role == Roles.Owner))
            throw new UnauthorizedAccessException("You are not authorized to do this");
        return trip;
    }

    private void ScheduleEmbeddingUpdate(int tripId)
    {
        _ = Task.Run(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();
            var tripRepository = scope.ServiceProvider.GetRequiredService<IOffroadTripRepository>();
            var trip = await tripRepository.GetTripById(tripId);
            if (trip == null) return;

            var routesText = string.Join(" ", trip.Routes.Select(r =>
                $"Days {r.StartDay}-{r.EndDay} {r.Name} {r.Note} {r.Source} {r.DistanceMeters}m {r.ElevationGainMeters}m"));
            var text =
                $"{trip.Title}. {trip.Description}. {string.Join(",", trip.Tags)}. {trip.Price}. {trip.MaxParticipants}. {routesText}";
            var embedding = await embeddingService.GetEmbedding(text);
            trip.UpdateEmbedding(new Vector(embedding));
            await tripRepository.SaveChanges();
        });
    }

    private async Task HandleMembershipRequest(int tripId, int invitedId)
    {
        var userId = _jwtService.GetUserId();
        var trip = await _tripRepository.GetTripById(tripId);
        var invited = await _userRepository.GetUserById(invitedId);
        if (invited == null) throw new KeyNotFoundException("User not found");
        if (trip == null) throw new KeyNotFoundException("Offroad trip not found");
        if (trip.Members.Any(m => m.UserId == invitedId)) throw new ArgumentException("User is already a member");
        if (trip.Members.Count >= trip.MaxParticipants) throw new AppException(402, "Trip is full");

        var owner = trip.Members.FirstOrDefault(m => m.Role == Roles.Owner);
        var ownerUser = owner != null ? await _userRepository.GetUserById(owner.UserId) : null;

        if (userId == invitedId)
        {
            trip.RequestMember(invitedId);
            if (ownerUser != null)
            {
                ownerUser.AddNotification($"{invited.Profile.Username} has requested to join {trip.Title}");
                _notificationService.SendNotificationAsync(owner.UserId, "Request",
                    $"{invited.Profile.Username} has requested to join your offroad trip",
                    $"/app/offroad/{trip.Id}?view=members");
            }
            trip.AddHistory($"{invited.Profile.Username} has requested to join the trip");
            await _userRepository.SaveChanges();
        }
        else
        {
            if (!trip.Members.Any(m => m.UserId == userId && (m.Role == Roles.Owner || m.Role == Roles.Admin)))
                throw new UnauthorizedAccessException("You are not authorized");
            trip.InivteMember(invitedId);
            invited.AddNotification($"You have been invited to join {trip.Title}");
            _notificationService.SendNotificationAsync(invitedId, "Invite",
                $"You have been invited to join {trip.Title}", "/app/profile?tab=invites");
            trip.AddHistory($"{invited.Profile.Username} has been invited to join the trip");
            await _userRepository.SaveChanges();
        }

        await _tripRepository.SaveChanges();
    }

    private async Task HandleMembershipResponse(int tripId, int invitedId, string status, string action)
    {
        if (action == null) throw new ArgumentException("Action is null");
        if (status == null) throw new ArgumentException("Status is null");

        var invited = await _userRepository.GetUserById(invitedId);
        var trip = await _tripRepository.GetTripById(tripId);
        if (trip == null) throw new KeyNotFoundException("Offroad trip not found");
        if (trip.Members.Count >= trip.MaxParticipants) throw new AppException(402, "Trip is full");

        var member = trip.Members.FirstOrDefault(m => m.UserId == invitedId);
        if (member == null) throw new KeyNotFoundException("Member not found");
        if (member.MemberStatus == MemberStatus.Accepted) throw new AppException(402, "Member already accepted");

        var owner = trip.Members.FirstOrDefault(m => m.Role == Roles.Owner);
        var ownerUser = owner != null ? await _userRepository.GetUserById(owner.UserId) : null;

        if (status.Equals(MemberStatus.Invited.ToString()))
        {
            if (action.Equals("accept"))
            {
                trip.AcceptMember(invitedId);
                trip.AddHistory($"{invitedId} has accepted the invitation");
                ownerUser?.AddNotification($"{invited.Profile.Username} has accepted your invitation");
                _notificationService.SendNotificationAsync(owner!.UserId, "Update on invitation",
                    $"{invited.Profile.Username} has accepted your invitation", $"/app/offroad/{trip.Id}?view=members");
            }
            else if (action.Equals("decline"))
            {
                trip.DeclineMember(invitedId);
                trip.AddHistory($"{invitedId} has declined the invitation");
                ownerUser?.AddNotification($"{invited.Profile.Username} has declined your invitation");
            }

            await _tripRepository.SaveChanges();
            await _userRepository.SaveChanges();
        }

        if (status.Equals(MemberStatus.Requested.ToString()))
        {
            var userId = _jwtService.GetUserId();
            var user = await _userRepository.GetUserById(userId);
            if (user == null) throw new KeyNotFoundException("User not found");
            if (!trip.Members.Any(m => m.UserId == userId && (m.Role == Roles.Owner || m.Role == Roles.Admin)))
                throw new UnauthorizedAccessException("You are not authorized");

            if (action.Equals("accept"))
            {
                trip.AcceptMember(invitedId);
                trip.AddHistory($"{user.Profile.Username} has accepted the request");
                invited.AddNotification($"You have been accepted to join {trip.Title}");
                await _userRepository.SaveChanges();
            }
            else if (action.Equals("decline"))
            {
                trip.DeclineMember(invitedId);
                trip.AddHistory($"{user.Profile.Username} has declined the request");
                invited.AddNotification($"You have been declined to join {trip.Title}");
                await _userRepository.SaveChanges();
            }

            await _tripRepository.SaveChanges();
        }
    }

    private async Task HandleRemoveMember(int tripId, int removedId)
    {
        var userId = _jwtService.GetUserId();
        var trip = await _tripRepository.GetTripById(tripId);
        if (trip == null) throw new KeyNotFoundException("Offroad trip not found");
        var remover = trip.Members.FirstOrDefault(m => m.UserId == userId);
        if (remover == null) throw new AppException(402, "You are not a member of this trip");
        if (remover.Role != Roles.Owner) throw new UnauthorizedAccessException("You are not authorized");
        var removedMember = trip.Members.FirstOrDefault(m => m.UserId == removedId);
        if (removedMember == null) throw new ArgumentException("User is not a member");
        if (removedMember.Role == Roles.Owner) throw new ArgumentException("You cannot remove the owner");
        var removed = await _userRepository.GetUserById(removedId);
        if (removed == null) throw new KeyNotFoundException("User not found");
        if (removed.Id == userId) throw new ArgumentException("You cannot remove yourself");
        trip.RemoveMember(removedId);
        trip.AddHistory($"{removed.Profile.Username} has been removed from the trip");
        await _tripRepository.SaveChanges();
    }

    private async Task HandleUpdateMember(UpdateRoleRequest updateRoleRequest)
    {
        var userId = _jwtService.GetUserId();
        var trip = await _tripRepository.GetTripById(updateRoleRequest.TripId);
        if (trip == null) throw new KeyNotFoundException("Offroad trip not found");
        var updater = trip.Members.FirstOrDefault(m => m.UserId == userId);
        if (updater == null) throw new ArgumentException("User is not a member");
        if (updater.Role != Roles.Owner) throw new UnauthorizedAccessException("You are not authorized");
        trip.UpdateMemberRole(updateRoleRequest.Id,
            updateRoleRequest.Role.Equals("admin", StringComparison.OrdinalIgnoreCase) ? Roles.Admin : Roles.Member);
        await _tripRepository.SaveChanges();
    }

    private static string? ImageContentType(string? fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return null;
        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            _ => "image/jpeg",
        };
    }
}
