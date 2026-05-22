using Microsoft.Extensions.DependencyInjection;
using Pgvector;
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

public class TripService : ITripService
{
    private readonly ITripRepository _tripRepository;
    private readonly ITripQueryService _tripQueryService;
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly IFileUploader _fileUploader;
    private readonly IMessageQueryService _messageQueryService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly INotificationService _notificationService;
    private readonly IPdfService _pdfService;
    private readonly IBackgroundModerationService _backgroundModeration;
    
    public TripService(ITripRepository tripRepository,ITripQueryService tripQueryService, IUserRepository userRepository, IJwtService jwtService, IFileUploader fileUploader, IMessageQueryService messageQueryService,IServiceScopeFactory scopeFactory, INotificationService notificationService, IPdfService pdfService, IBackgroundModerationService backgroundModeration)
    {
        _tripRepository = tripRepository;
        _tripQueryService = tripQueryService;
        _userRepository = userRepository;
        _jwtService = jwtService;
        _fileUploader = fileUploader;
        _messageQueryService = messageQueryService;
        _scopeFactory = scopeFactory;
        _notificationService = notificationService;
        _pdfService = pdfService;
        _backgroundModeration = backgroundModeration;
    }

    public async Task<int> CreateTrip(TripRequest tripRequest)
    {
        if(tripRequest == null) throw new ArgumentNullException("Trip request is null");
        int userId = _jwtService.GetUserId();
        var trip = Trip.Create(tripRequest.Title, tripRequest.Description, tripRequest.StartingDate,
            tripRequest.EndingDate, tripRequest.Tags, tripRequest.MaxParticipants, tripRequest.Price, userId);
        foreach (var timelineRequest in tripRequest.Timelines)
        {
            var timeline = trip.AddTimeline(timelineRequest.StartDay,timelineRequest.EndDay, timelineRequest.StartingPoint, timelineRequest.FromCoords, timelineRequest.EndPoint, timelineRequest.ToCoords, timelineRequest.Note);
            foreach (var activity in timelineRequest.Activities)
            {
                TripActivity tripActivity = new TripActivity(activity.Name, activity.Description, activity.Link,
                    activity.Cost, activity.Type);
                timeline.AddActivity(tripActivity);
            }
        }
        

        var imageBytes = await StreamBuffer.ReadAllBytesAsync(tripRequest.ImageStream);

        await _tripRepository.CreateTrip(trip);
        await _tripRepository.SaveChanges();
        if (tripRequest.ImageStream != null)
        {
            string url = await _fileUploader.UploadFile(tripRequest.ImageStream,Path.GetExtension(tripRequest.ImageFileName),"trip", trip.Id);
            trip.SetImageUrl(url);
        }
        else if (!string.IsNullOrWhiteSpace(tripRequest.ImageUrl))
        {
            trip.SetImageUrl(tripRequest.ImageUrl);
        }
        await _tripRepository.SaveChanges();

        _backgroundModeration.ScheduleTextReview(
            ModerationTarget.TripDetails,
            userId,
            trip.Id,
            ModerationFields.ToReviewList(ModerationFields.FromTripRequest(tripRequest)));
        if (imageBytes is { Length: > 0 })
            _backgroundModeration.ScheduleImageReview(
                ModerationTarget.TripCover, userId, trip.Id, imageBytes, ImageContentType(tripRequest.ImageFileName));
        _ = Task.Run(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();
            var tripRepository = scope.ServiceProvider.GetRequiredService<ITripRepository>();
            string timelineString = "";
            string activitesString = "Activites:";
            foreach (var timeline in trip.Timelines)
            {
                timelineString += $"{timeline.StartDay},{timeline.EndDay}, {timeline.StartingPoint}, {timeline.FromCoords}, {timeline.EndPoint}, {timeline.ToCoords}, {timeline.Note}, ";
                
                foreach (var activity in timeline.Activities)
                {
                    activitesString += $"{activity.Name}, {activity.Description}, {activity.Link}, {activity.Cost}, {activity.Type}, ";
                }
                timelineString += activitesString;
            }

            var text =
                $"{trip.Title}. {trip.Description}. {string.Join(",", trip.Tags)}. {trip.Price}. {trip.MaxParticipants}. {timelineString}";
            var embedding = await embeddingService.GetEmbedding(text);
            var freshTrip = await tripRepository.GetTripById(trip.Id);
            freshTrip.UpdateEmbedding(new Vector(embedding));
            await tripRepository.SaveChanges();
        });
        return trip.Id;
    }


    public async Task<List<TripResponse>> GetTripsForUser(TripsRequest tripsRequest)
    {
        int userId = _jwtService.GetUserId();
        if(userId == null) throw new KeyNotFoundException("User not found");
        var trips = await _tripQueryService.GetTripsForUser(userId, tripsRequest);
        
        return trips;
    }

    public async Task<List<TripResponse>> GetTrips()
    {
        int userId = _jwtService.GetUserId();
        var trips = await _tripQueryService.GetTrips(userId);
        return trips;
    }
    
    public async Task<TripResponse> GetTrip(int tripId)
    {
        var trip = await _tripQueryService.GetTrip(tripId, _jwtService.GetUserId());
        if(trip == null) throw new KeyNotFoundException("Trip not found");
        return trip;
    }

    public async Task MembershipRequest(int tripId, int invitedId)
    {
        int userId = _jwtService.GetUserId();
        var trip = await _tripRepository.GetTripById(tripId);
        var invited = await _userRepository.GetUserById(invitedId);
        if(invited == null) throw new KeyNotFoundException("User not found");
        if(trip == null) throw new KeyNotFoundException("Trip not found");
        if(trip.Members.Any(m => m.UserId == invitedId)) throw new ArgumentException("User is already a member of this trip");
        if(trip.Members.Count == trip.MaxParticipants) throw new AppException(402,"Trip is full");
        int ownerId = trip.Members.FirstOrDefault(m => m.Role == Roles.Owner).UserId;
        var owner = await _userRepository.GetUserById(ownerId);
        if (userId == invitedId)
        {
            trip.RequestMember(invitedId);
            if(owner != null) owner.AddNotification($"{invited.Profile.Username} has requested to join {trip.Title}");
            _notificationService.SendNotificationAsync(ownerId, "Request",
                $"{invited.Profile.Username} has requested to join your trip", $"/app/trip/{trip.Id}?view=members"); 
            trip.AddHistory($"{invited.Profile.Username} has requested to join the trip");
            await _userRepository.SaveChanges();
        }
        else
        {
            if (!trip.Members.Any(m => m.UserId == userId && (m.Role == Roles.Owner || m.Role == Roles.Admin))) throw new UnauthorizedAccessException("You are not authorized");
            trip.InivteMember(invitedId);
            invited.AddNotification($"You have been invited to join {trip.Title} by {owner.Profile.Username}");
            _notificationService.SendNotificationAsync(invitedId, "Invite",
                $"You have been invited to join {trip.Title} by {owner.Profile.Username}", $"/app/profile?tab=invites");
            trip.AddHistory($"{invited.Profile.Username} has been invited to join the trip");
            await _userRepository.SaveChanges();
        }
        await _tripRepository.SaveChanges();
    }

    public async Task MembershipResponse(int tripId, int invitedId, string status,string action)
    {
        Console.WriteLine(status);
        Console.WriteLine(action);
        if(action == null) throw new ArgumentException("Action is null");
        if(status == null) throw new ArgumentException("Status is null");
        var invited = await _userRepository.GetUserById(invitedId);
        var trip = await _tripRepository.GetTripById(tripId);
        int ownerId = trip.Members.FirstOrDefault(m => m.Role == Roles.Owner).UserId;
        var owner = await _userRepository.GetUserById(ownerId);
        if(trip == null) throw new KeyNotFoundException("Trip not found");
        if(trip.Members.Count == trip.MaxParticipants) throw new AppException(402,"Trip is full");
        var member = trip.Members.FirstOrDefault(m => m.UserId == invitedId);
        if(member == null) throw new KeyNotFoundException("Member not found");
        if(member.MemberStatus == MemberStatus.Accepted) throw new AppException(402,"Member already accepted");
        if (status.Equals(MemberStatus.Invited.ToString()))
        {
            
            if (action.Equals("accept"))
            {
                trip.AcceptMember(invitedId);
                trip.AddHistory($"{invitedId} has accepted the invitation");
                owner.AddNotification($"{invited.Profile.Username} has accepted your invitation");
                _notificationService.SendNotificationAsync(ownerId, "Update on invitation",
                    $"{invited.Profile.Username} has accepted your invitation", $"/app/trip/{trip.Id}?view=members");
            }

            if (action.Equals("decline"))
            {
                trip.DeclineMember(invitedId);
                trip.AddHistory($"{invitedId} has declined the invitation");
                owner.AddNotification($"{invited.Profile.Username} has declined your invitation");
                _notificationService.SendNotificationAsync(ownerId, "Update on invitation",
                    $"{invited.Profile.Username} has declined your invitation", $"/app/trip/{trip.Id}?view=members");
            }
            await _tripRepository.SaveChanges();
            await _userRepository.SaveChanges();
        }

        if (status.Equals(MemberStatus.Requested.ToString()))
        {
            int userId = _jwtService.GetUserId();
            var user = await _userRepository.GetUserById(userId);
            if(user == null) throw new KeyNotFoundException("User not found");
            if (!trip.Members.Any(m => m.UserId == userId && (m.Role == Roles.Owner || m.Role == Roles.Admin))) throw new UnauthorizedAccessException("You are not authorized");
            if (action.Equals("accept"))
            {
                trip.AcceptMember(invitedId);
                trip.AddHistory($"{user.Profile.Username} has accepted your request");
                invited.AddNotification($"You have been accepted to join {trip.Title} by {owner.Profile.Username}");
                _notificationService.SendNotificationAsync(invitedId, "Update on request",
                    $"You have been accepted to join {trip.Title} by {owner.Profile.Username}", $"/app/trip/{trip.Id}?view=members");
                await _userRepository.SaveChanges();
            }

            if (action.Equals("decline"))
            {
                trip.DeclineMember(invitedId);
                trip.AddHistory($"{user.Profile.Username} has declined your request");
                invited.AddNotification($"You have been declined to join {trip.Title} by {owner.Profile.Username}");
                _notificationService.SendNotificationAsync(invitedId, "Update on request",
                    $"You have been declined to join {trip.Title} by {owner.Profile.Username}", $"/app/trip/{trip.Id}?view=members");
                await _userRepository.SaveChanges();
            }
        }
    }
    public async Task RemoveMember(int tripId, int removedId)
    {
        int userId = _jwtService.GetUserId();
        Console.WriteLine(userId);
        var trip = await _tripRepository.GetTripById(tripId);
        if(trip == null) throw new KeyNotFoundException("Trip not found");
        var remover = trip.Members.FirstOrDefault(m => m.UserId == userId);
        if(remover == null) throw new AppException(402,"You are not a member of this trip");
        if(remover.Role != Roles.Owner) throw new UnauthorizedAccessException("You are not authorized to do this");
        var removedTrip = trip.Members.FirstOrDefault(m => m.UserId == removedId);
        if(removedTrip == null) throw new ArgumentException("User is not a member of this trip");
        if(removedTrip.Role == Roles.Owner) throw new ArgumentException("You cannot remove the owner of the trip");
        var removed = await _userRepository.GetUserById(removedId);
        if(removed == null) throw new KeyNotFoundException("User not found");
        if(removed.Id == userId) throw new ArgumentException("You cannot remove yourself from the trip");
        trip.RemoveMember(removedId);
        trip.AddHistory($"{removed.Profile.Username} has been removed from the trip");
        await _tripRepository.SaveChanges();
    }
    public async Task UpdateMember(UpdateRoleRequest updateRoleRequest)
    {
        Console.WriteLine(updateRoleRequest.TripId);
        int userId = _jwtService.GetUserId();
        int tripId = updateRoleRequest.TripId;
        int updatedId = updateRoleRequest.Id;
        string role = updateRoleRequest.Role;
        var trip = await _tripRepository.GetTripById(tripId);
        if(trip == null) throw new KeyNotFoundException("Trip not found");
        var updater = trip.Members.FirstOrDefault(m => m.UserId == userId);
        if(updater == null) throw new ArgumentException("User is not a member of this trip");
        if(updater.Role != Roles.Owner) throw new UnauthorizedAccessException("You are not authorized to do this");
        if (role.Equals("admin"))
        {
            trip.UpdateMemberRole(updatedId, Roles.Admin);
        }
        else
        {
            trip.UpdateMemberRole(updatedId, Roles.Member);
        }
        await _tripRepository.SaveChanges();
    }
    public async Task<TripResponse> UpdateTrip(UpdateTripRequest UpdateTripRequest)
    {
        if(UpdateTripRequest == null) throw new ArgumentNullException("Trip request is null");
        int userId = _jwtService.GetUserId();
        var trip = await _tripRepository.GetTripById(UpdateTripRequest.Id);
        if(trip == null) throw new KeyNotFoundException("Trip not found");
        bool isOwner = trip.Members.Any(m => m.UserId == userId && m.Role == Roles.Owner);
        if(!isOwner) throw new UnauthorizedAccessException("You are not authorized to do this");
        var imageBytes = await StreamBuffer.ReadAllBytesAsync(UpdateTripRequest.ImageStream);

        trip.UpdateTrip(UpdateTripRequest.Title, UpdateTripRequest.Description, UpdateTripRequest.StartingDate, UpdateTripRequest.EndingDate, UpdateTripRequest.Status,UpdateTripRequest.Tags, UpdateTripRequest.MaxParticipants, UpdateTripRequest.Price);
        if (UpdateTripRequest.ImageStream != null)
        {
            string url = await _fileUploader.UploadFile(UpdateTripRequest.ImageStream,Path.GetExtension(UpdateTripRequest.ImageFileName),"trip", trip.Id);
            trip.SetImageUrl(url);
        }
        await _tripRepository.SaveChanges();

        _backgroundModeration.ScheduleTextReview(
            ModerationTarget.TripDetails,
            userId,
            trip.Id,
            ModerationFields.ToReviewList(ModerationFields.FromTripUpdate(UpdateTripRequest)));
        if (imageBytes is { Length: > 0 })
            _backgroundModeration.ScheduleImageReview(
                ModerationTarget.TripCover, userId, trip.Id, imageBytes, ImageContentType(UpdateTripRequest.ImageFileName));
        _ = Task.Run(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();
            var tripRepository = scope.ServiceProvider.GetRequiredService<ITripRepository>();
            string timelineString = "";
            string activitesString = "Activites:";
            foreach (var timeline in trip.Timelines)
            {
                timelineString += $"{timeline.StartDay},{timeline.EndDay}, {timeline.StartingPoint}, {timeline.FromCoords}, {timeline.EndPoint}, {timeline.ToCoords}, {timeline.Note}, ";
                
                foreach (var activity in timeline.Activities)
                {
                    activitesString += $"{activity.Name}, {activity.Description}, {activity.Link}, {activity.Cost}, {activity.Type}, ";
                }
                timelineString += activitesString;
            }

            var text =
                $"{trip.Title}. {trip.Description}. {string.Join(",", trip.Tags)}. {trip.Price}. {trip.MaxParticipants}. {timelineString}";
            var embedding = await embeddingService.GetEmbedding(text);
            var freshTrip = await tripRepository.GetTripById(trip.Id);
            freshTrip.UpdateEmbedding(new Vector(embedding));
            await tripRepository.SaveChanges();
        });
        return await _tripQueryService.GetTrip(trip.Id,userId);
        
    }

    public async Task<TripTimelineResponse> GetTimeline(int tripId,int id)
    {
        int userId = _jwtService.GetUserId();
        var trip = await _tripRepository.GetTripById(tripId);
        if(trip == null) throw new KeyNotFoundException("Trip not found");
        bool isOwner = trip.Members.Any(m => m.UserId == userId && m.Role == Roles.Owner);
        if(!isOwner) throw new UnauthorizedAccessException("You are not authorized to view this");
        return await _tripQueryService.GetTimeline(id);
    }

    public async Task<TripTimelineResponse> UpdateTimeline(UpdateTimelineRequest updateTimelineRequest)
    {
        int userId = _jwtService.GetUserId();
        int tripId = updateTimelineRequest.TripId;
        int id = updateTimelineRequest.Id;
        var trip = await _tripRepository.GetTripById(tripId);
        if(trip == null) throw new KeyNotFoundException("Trip not found");
        bool isOwner = trip.Members.Any(m => m.UserId == userId && m.Role == Roles.Owner);
        if(!isOwner) throw new KeyNotFoundException("You are not authorized to view this");
        trip.UpdateTimeline(updateTimelineRequest.Id, updateTimelineRequest.StartDay,updateTimelineRequest.EndDay, updateTimelineRequest.StartingPoint, updateTimelineRequest.FromCoords, updateTimelineRequest.EndPoint, updateTimelineRequest.ToCoords, updateTimelineRequest.Note);
        await _tripRepository.SaveChanges();

        _backgroundModeration.ScheduleTextReview(
            ModerationTarget.TripTimeline,
            userId,
            tripId,
            ModerationFields.ToReviewList(ModerationFields.FromTimeline(updateTimelineRequest)),
            updateTimelineRequest.Id);

        _ = Task.Run(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();
            var tripRepository = scope.ServiceProvider.GetRequiredService<ITripRepository>();
            string timelineString = "";
            string activitesString = "Activites:";
            foreach (var timeline in trip.Timelines)
            {
                timelineString += $"{timeline.StartDay},{timeline.EndDay}, {timeline.StartingPoint}, {timeline.FromCoords}, {timeline.EndPoint}, {timeline.ToCoords}, {timeline.Note}, ";
                
                foreach (var activity in timeline.Activities)
                {
                    activitesString += $"{activity.Name}, {activity.Description}, {activity.Link}, {activity.Cost}, {activity.Type}, ";
                }
                timelineString += activitesString;
            }

            var text =
                $"{trip.Title}. {trip.Description}. {string.Join(",", trip.Tags)}. {trip.Price}. {trip.MaxParticipants}. {timelineString}";
            var embedding = await embeddingService.GetEmbedding(text);
            var freshTrip = await tripRepository.GetTripById(trip.Id);
            freshTrip.UpdateEmbedding(new Vector(embedding));
            await tripRepository.SaveChanges();
        });
        return await _tripQueryService.GetTimeline(id);
    }

    public async Task RemoveTimeline(int tripId,int id)
    {
        int userId = _jwtService.GetUserId();
        var user = await _userRepository.GetUserById(userId);
        if(user == null) throw new KeyNotFoundException("User not found");
        var trip = await _tripRepository.GetTripById(tripId);
        if(trip == null) throw new KeyNotFoundException("Trip not found");
        bool isOwner = trip.Members.Any(m => m.UserId == userId && m.Role == Roles.Owner);
        if(!isOwner) throw new UnauthorizedAccessException("You are not authorized to do this");
        trip.RemoveTimeline(id);
        await _tripRepository.SaveChanges();
        _ = Task.Run(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();
            var tripRepository = scope.ServiceProvider.GetRequiredService<ITripRepository>();
            string timelineString = "";
            string activitesString = "Activites:";
            foreach (var timeline in trip.Timelines)
            {
                timelineString += $"{timeline.StartDay},{timeline.EndDay}, {timeline.StartingPoint}, {timeline.FromCoords}, {timeline.EndPoint}, {timeline.ToCoords}, {timeline.Note}, ";
                
                foreach (var activity in timeline.Activities)
                {
                    activitesString += $"{activity.Name}, {activity.Description}, {activity.Link}, {activity.Cost}, {activity.Type}, ";
                }
                timelineString += activitesString;
            }

            var text =
                $"{trip.Title}. {trip.Description}. {string.Join(",", trip.Tags)}. {trip.Price}. {trip.MaxParticipants}. {timelineString}";
            var embedding = await embeddingService.GetEmbedding(text);
            var freshTrip = await tripRepository.GetTripById(trip.Id);
            freshTrip.UpdateEmbedding(new Vector(embedding));
            await tripRepository.SaveChanges();
        });
    }

    public async Task AddTimeline(UpdateTimelineRequest updateTimelineRequest)
    {
        int userId = _jwtService.GetUserId();
        int tripId = updateTimelineRequest.TripId;
        var user = await _userRepository.GetUserById(userId);
        if(user == null) throw new KeyNotFoundException("User not found");
        var trip = await _tripRepository.GetTripById(tripId);
        if(trip == null) throw new KeyNotFoundException("Trip not found");
        bool isOwner = trip.Members.Any(m => m.UserId == userId && m.Role == Roles.Owner);
        if(!isOwner) throw new UnauthorizedAccessException("You are not authorized to do this");
        trip.AddTimeline(updateTimelineRequest.StartDay,updateTimelineRequest.EndDay, updateTimelineRequest.StartingPoint, updateTimelineRequest.FromCoords, updateTimelineRequest.EndPoint, updateTimelineRequest.ToCoords, updateTimelineRequest.Note);
        await _tripRepository.SaveChanges();

        var timelineId = trip.Timelines.MaxBy(t => t.Id)?.Id ?? 0;
        if (timelineId > 0)
        {
            _backgroundModeration.ScheduleTextReview(
                ModerationTarget.TripTimeline,
                userId,
                tripId,
                ModerationFields.ToReviewList(ModerationFields.FromTimeline(updateTimelineRequest)),
                timelineId);
        }

        _ = Task.Run(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();
            var tripRepository = scope.ServiceProvider.GetRequiredService<ITripRepository>();
            string timelineString = "";
            string activitesString = "Activites:";
            foreach (var timeline in trip.Timelines)
            {
                timelineString += $"{timeline.StartDay},{timeline.EndDay}, {timeline.StartingPoint}, {timeline.FromCoords}, {timeline.EndPoint}, {timeline.ToCoords}, {timeline.Note}, ";
                
                foreach (var activity in timeline.Activities)
                {
                    activitesString += $"{activity.Name}, {activity.Description}, {activity.Link}, {activity.Cost}, {activity.Type}, ";
                }
                timelineString += activitesString;
            }

            var text =
                $"{trip.Title}. {trip.Description}. {string.Join(",", trip.Tags)}. {trip.Price}. {trip.MaxParticipants}. {timelineString}";
            var embedding = await embeddingService.GetEmbedding(text);
            var freshTrip = await tripRepository.GetTripById(trip.Id);
            freshTrip.UpdateEmbedding(new Vector(embedding));
            await tripRepository.SaveChanges();
        });
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

    public async Task<List<MessageResponse>> GetMessages(int tripId)
    {
        return await _messageQueryService.GetMessages(tripId); 
    }

    public async Task<byte[]> ExportCosts(int tripId)
    {
        var trip = await _tripRepository.GetTripById(tripId);
        if (trip == null) throw new KeyNotFoundException("Trip not found");
        return _pdfService.GenerateCostsPdf(trip);
    }
    
}