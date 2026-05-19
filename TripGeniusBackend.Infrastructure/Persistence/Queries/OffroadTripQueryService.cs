using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TripGeniusBackend.Application.DTOs.OffroadTrip;
using TripGeniusBackend.Application.DTOs.Trip;
using TripGeniusBackend.Application.Interfaces.Queries;
using TripGeniusBackend.Domain.Entities;
using TripGeniusBackend.Domain.Enums;

namespace TripGeniusBackend.Infrastructure.Persistence.Queries;

public class OffroadTripQueryService : IOffroadTripQueryService
{
    private readonly AppDbContext _context;

    public OffroadTripQueryService(AppDbContext context) => _context = context;

    public async Task<OffroadTripResponse?> GetTrip(int id, int userId) =>
        await _context.OffroadTrips.Where(t => t.Id == id).Select(MapToResponse(userId)).FirstOrDefaultAsync();

    public async Task<List<OffroadTripResponse>> GetTrips(int userId) =>
        await _context.OffroadTrips.Select(MapToResponse(userId)).ToListAsync();

    public async Task<List<OffroadTripResponse>> GetTripsForUser(int userId, OffroadTripsRequest request)
    {
        var user = await _context.Users.Where(u => u.Id == userId).Select(u => new { u.Preferences }).FirstOrDefaultAsync();
        if (user == null) throw new ArgumentException("User not found");
        var search = request.Search?.ToLower() ?? "";

        var query = _context.OffroadTrips.Where(t =>
            t.Status == Status.Upcoming &&
            t.Price <= request.Budget &&
            t.MaxParticipants > t.Members.Count &&
            t.Title.ToLower().Contains(search));

        if (request.Preferences)
            query = query.Where(t =>
                user.Preferences.Tags.Any(tag => t.Tags.Contains(tag)) &&
                t.MaxParticipants <= user.Preferences.MaxGroupSize);
        else if (!request.Tag.Equals("all", StringComparison.OrdinalIgnoreCase))
            query = query.Where(t => t.Tags.Contains(request.Tag));

        return await query.Select(MapToResponse(userId)).ToListAsync();
    }

    public async Task<OffroadRouteResponse?> GetRoute(int routeId) =>
        await _context.OffroadRoutes.Where(r => r.Id == routeId).Select(MapToRouteResponse()).FirstOrDefaultAsync();

    private static Expression<Func<OffroadTrip, OffroadTripResponse>> MapToResponse(int userId) =>
        t => new OffroadTripResponse
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
            Routes = t.Routes.Select(r => new OffroadRouteResponse
            {
                Id = r.Id,
                StartDay = r.StartDay,
                EndDay = r.EndDay,
                Name = r.Name,
                Note = r.Note,
                TrackGeoJson = r.TrackGeoJson,
                Source = r.Source.ToString(),
                DistanceMeters = r.DistanceMeters,
                ElevationGainMeters = r.ElevationGainMeters
            }).ToList(),
            Members = t.Members.Select(m => new TripMemberResponse
            {
                Id = m.UserId,
                Role = m.Role.ToString().ToLowerInvariant(),
                Username = m.User.Profile.Username,
                AvatarUrl = m.User.Profile.ProfileURL,
                MemberStatus = m.MemberStatus.ToString().ToLowerInvariant()
            }).ToList(),
            History = t.History.OrderBy(h => h.Date).Select(h => new TripHistoryResponse
            {
                Id = h.Id,
                Date = h.Date,
                Content = h.Content
            }).ToList(),
            IsUserMember = t.Members.Any(m => m.UserId == userId)
        };

    private static Expression<Func<OffroadRoute, OffroadRouteResponse>> MapToRouteResponse() =>
        r => new OffroadRouteResponse
        {
            Id = r.Id,
            StartDay = r.StartDay,
            EndDay = r.EndDay,
            Name = r.Name,
            Note = r.Note,
            TrackGeoJson = r.TrackGeoJson,
            Source = r.Source.ToString(),
            DistanceMeters = r.DistanceMeters,
            ElevationGainMeters = r.ElevationGainMeters
        };
}
