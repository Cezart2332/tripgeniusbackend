using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TripGeniusBackend.Application.DTOs.Trip;
using TripGeniusBackend.Application.Interfaces.Queries;
using TripGeniusBackend.Domain.Entities;

namespace TripGeniusBackend.Infrastructure.Persistence.Queries;

public class TripQueryService : ITripQueryService
{
    private readonly AppDbContext _context;

    public TripQueryService(AppDbContext context)
    {
        _context = context;
    }
    public async Task<TripResponse?> GetTripById(int id,int userId)
    {
        var trip = await _context.Trips.Where(t => t.Id == id).Select(MapToTripResponse(userId)).FirstOrDefaultAsync();
        if(trip == null) return null;
        return trip;
    }

    public async Task<List<TripResponse>> GetTrips(int userId)
    {
        return await _context.Trips.Select(MapToTripResponse(userId)).ToListAsync();
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
            Timelines = t.Timelines.Select(timeline => new TripTimelineRequest
            {
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
                AvatarUrl = member.User.Profile.ProfileURL
            }).ToList(),
            isUserMember = t.Members.Any(m => m.UserId == userId)
        };
    }
}