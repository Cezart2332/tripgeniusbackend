using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TripGeniusBackend.Application.DTOs.Notifications;
using TripGeniusBackend.Application.DTOs.Trip;
using TripGeniusBackend.Application.DTOs.User;
using TripGeniusBackend.Application.Interfaces;
using TripGeniusBackend.Application.Interfaces.Queries;
using TripGeniusBackend.Domain.Entities;
using TripGeniusBackend.Infrastructure.Persistence.Services;

namespace TripGeniusBackend.Infrastructure.Persistence.Queries;

public class UserQueryService : IUserQueryService
{
    private readonly AppDbContext _context;
    private readonly IJwtService _jwtService;

    public UserQueryService(AppDbContext context, IJwtService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
    }


    public async Task <List<UserResponse>> GetUserByUsername(UsersRequest usersRequest)
    {
        int userId = _jwtService.GetUserId();
        var searchTerm = usersRequest.Username?.ToLower() ?? "";
        if (searchTerm.Trim().Equals("")) return null;
        var query = await _context.Users.Where(u => u.Id != userId && u.Profile.Username.ToLower().Contains(usersRequest.Username.ToLower())).Select(MapToUserResponse()).ToListAsync();
        return query;
    }
    public async Task<UserResponse?> GetUserDetails(int id)
    {
        return await _context.Users.Where(u => u.Id == id).Select(MapToUserResponse()).FirstOrDefaultAsync();
    }

    private static Expression<Func<User, UserResponse>> MapToUserResponse()
    {
        return user => new UserResponse
        {
            Id = user.Id,
            Username = user.Profile.Username,
            Email = user.Email,
            IsVerified = user.IsVerified,
            ProfileUrl = user.Profile.ProfileURL,
            Description = user.Profile.Description,
            Tags = user.Preferences.Tags,
            GroupSize = user.Preferences.MaxGroupSize,
            Notifications = user.Notifications.Select(notification => new NotificationResponse
            {
                Id = notification.Id,
                Content = notification.Content,
                CreatedAt = notification.CreatedAt,
                IsRead = notification.IsRead
            }).ToList(),
            Trips = user.Trips.Select(tm => tm.Trip).Select(trip => new TripResponse
            {
                Id = trip.Id,
                Title = trip.Title,
                Description = trip.Description,
                ImageUrl = trip.ImageUrl,
                Status = trip.Status.ToString(),
                StartingDate = trip.StartDate,
                EndingDate = trip.EndDate,
                Price = trip.Price,
                CurrentMembers = trip.Members.Count,
                MaxParticipants = trip.MaxParticipants,
                Tags = trip.Tags,
                Timelines = trip.Timelines.Select(timeline => new TripTimelineResponse  
                {
                    Id = timeline.Id,
                    StartDay = timeline.StartDay,
                    EndDay = timeline.EndDay,
                    StartingPoint = timeline.StartingPoint,
                    EndPoint = timeline.EndPoint,
                    FromCoords = timeline.FromCoords,
                    ToCoords = timeline.ToCoords,
                    Note = timeline.Note,
                    Activities = timeline.Activities.Select(a => new TripActivityRequest
                        {
                        Name = a.Name,
                        Description = a.Description,
                        Cost = a.Cost,
                        Link = a.Link,
                        Type = a.Type
                        }).ToList()
                }).ToList(),
                Members = trip.Members.Select(member => new TripMemberResponse
                {
                    Id = member.UserId,
                    Role = member.Role.ToString(),
                    Username = member.User.Profile.Username,
                    AvatarUrl = member.User.Profile.ProfileURL,
                    MemberStatus = member.MemberStatus.ToString()
                }).ToList(),
            }).ToList()
        };
    }
}