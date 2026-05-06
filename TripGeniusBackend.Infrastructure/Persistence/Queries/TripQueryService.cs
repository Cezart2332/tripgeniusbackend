using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TripGeniusBackend.Application.DTOs.Trip;
using TripGeniusBackend.Application.Interfaces.Queries;
using TripGeniusBackend.Domain.Entities;
using TripGeniusBackend.Domain.Enums;

namespace TripGeniusBackend.Infrastructure.Persistence.Queries;

public class TripQueryService : ITripQueryService
{
    private readonly AppDbContext _context;

    public TripQueryService(AppDbContext context)
    {
        _context = context;
    }
    public async Task<TripResponse?> GetTrip(int id,int userId)
    {
        var trip = await _context.Trips.Where(t => t.Id == id).Select(MapToTripResponse(userId)).FirstOrDefaultAsync();
        if(trip == null) return null;
        return trip;
    }

    public async Task<List<TripResponse>> GetTrips(int userId)
    {
        return await _context.Trips.Select(MapToTripResponse(userId)).ToListAsync();
    }

    public async Task<List<TripResponse>> GetUserTrips(int userId)
    {
        return await _context.Trips.Where(t => t.Members.Any(m => m.UserId == userId)).Select(MapToTripResponse(userId)).ToListAsync();
    }
    public async Task<List<TripResponse>> GetTripsForUser(int userId, TripsRequest tripsRequest)
    {
        var user = await _context.Users.Where(u => u.Id == userId).Select(u => new {u.Preferences}).FirstOrDefaultAsync();
        if(user == null) throw new ArgumentException("User not found");
        string search = tripsRequest.Search?.ToLower() ?? "";
        
        var query =  _context.Trips.Where(t => t.Status == Status.Upcoming && t.Price <= tripsRequest.Budget && t.MaxParticipants > t.Members.Count  && t.Title.ToLower().Contains(search));
        if(tripsRequest.Preferences) query = query.Where(t => user.Preferences.Tags.Any(tag => t.Tags.Contains(tag)) && t.MaxParticipants <= user.Preferences.MaxGroupSize);
        else if(!tripsRequest.Tag.Equals("all")) query = query.Where(t => t.Tags.Contains(tripsRequest.Tag));
        return await query.Select(MapToTripResponse(userId)).ToListAsync();
    }

    public async Task<TripMember> GetMember(int tripId, int userId)
    {
        return await _context.TripMembers.Where(tm => tm.TripId == tripId && tm.UserId == userId).FirstOrDefaultAsync();
    }
    
    public async Task<TripTimelineResponse> GetTimeline(int tripTimelineId)
    {
        return await _context.TripTimelines.Where(t => t.Id == tripTimelineId).Select(MapToTripTimelineResponse(tripTimelineId)).FirstOrDefaultAsync();
    }
    private static Expression<Func<Trip,TripResponse>> MapToTripResponse(int userId)
    {
        return t => new TripResponse
        {
            Id = t.Id,
            Title = t.Title,
            Description = t.Description,
            ImageUrl = t.ImageUrl,
            Status = t.Status.ToString(),
            StartingDate = t.StartDate,
            EndingDate = t.EndDate,
            Price = t.Price,
            CurrentMembers = t.Members.Count,
            MaxParticipants = t.MaxParticipants,
            Tags = t.Tags,
            Timelines = t.Timelines.Select(timeline => new TripTimelineResponse
            {
                Id = timeline.Id,
                Day = timeline.Day,
                StartingPoint = timeline.StartingPoint,
                EndPoint = timeline.EndPoint,
                FromCoords = timeline.FromCoords,
                ToCoords = timeline.ToCoords,
                Note = timeline.Note
            }).ToList(),
            Members = t.Members.Select(member => new TripMemberResponse
            {
                Id = member.UserId,
                Role = member.Role.ToString(),
                Username = member.User.Profile.Username,
                AvatarUrl = member.User.Profile.ProfileURL,
                MemberStatus = member.MemberStatus.ToString()
            }).ToList(),
            History = t.History.OrderBy(th => th.Date).Select(history => new TripHistoryResponse
            {
                Id = history.Id,
                Date = history.Date,
                Content = history.Content
            }).ToList(),
            IsUserMember = t.Members.Any(m => m.UserId == userId)
        };
    }
    
    private static Expression<Func<TripTimeline,TripTimelineResponse>> MapToTripTimelineResponse(int tripTimelineId)
    {
        return t => new TripTimelineResponse
        {
            Id = t.Id,
            Day = t.Day,
            StartingPoint = t.StartingPoint,
            EndPoint = t.EndPoint,
            FromCoords = t.FromCoords,
            ToCoords = t.ToCoords,
            Note = t.Note
        };
    }
}