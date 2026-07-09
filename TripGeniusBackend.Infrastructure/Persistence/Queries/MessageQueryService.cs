using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TripGeniusBackend.Application.DTOs.Trip;
using TripGeniusBackend.Application.Interfaces.Queries;
using TripGeniusBackend.Domain.Entities;
using TripGeniusBackend.Domain.Enums;

namespace TripGeniusBackend.Infrastructure.Persistence.Queries;

public class MessageQueryService : IMessageQueryService
{
    private readonly AppDbContext _context;

    public MessageQueryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<MessageResponse>> GetMessages(int tripId)
    {
        return await _context.Messages.Where(m => m.TripId == tripId).OrderBy(m => m.Date).Select(MapToMessageResponse()).ToListAsync();
    }
    
    
    private static Expression<Func<Message, MessageResponse>> MapToMessageResponse()
    {
        return message => new MessageResponse
        {
            Id = message.Id,
            Content = message.Content,
            SentAt = message.Date,
            ImageUrl = message.ImageURL,
            Username = message.SenderType == SenderType.Ai ? "TripGenius AI" : message.User!.Profile.Username,
            ProfileUrl = message.SenderType == SenderType.Ai ? "" : message.User!.Profile.ProfileURL,
            IsAi = message.SenderType == SenderType.Ai
        };
    }
}