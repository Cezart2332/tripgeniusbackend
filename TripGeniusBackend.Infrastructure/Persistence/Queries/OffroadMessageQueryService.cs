using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TripGeniusBackend.Application.DTOs.Trip;
using TripGeniusBackend.Application.Interfaces.Queries;
using TripGeniusBackend.Domain.Entities;

namespace TripGeniusBackend.Infrastructure.Persistence.Queries;

public class OffroadMessageQueryService : IOffroadMessageQueryService
{
    private readonly AppDbContext _context;

    public OffroadMessageQueryService(AppDbContext context) => _context = context;

    public async Task<List<MessageResponse>> GetMessages(int offroadTripId) =>
        await _context.OffroadMessages
            .Where(m => m.OffroadTripId == offroadTripId)
            .OrderBy(m => m.Date)
            .Select(MapToMessageResponse())
            .ToListAsync();

    private static Expression<Func<OffroadMessage, MessageResponse>> MapToMessageResponse() =>
        m => new MessageResponse
        {
            Id = m.Id,
            Content = m.Content,
            SentAt = m.Date,
            ImageUrl = m.ImageURL,
            Username = m.User.Profile.Username,
            ProfileUrl = m.User.Profile.ProfileURL
        };
}
