using Microsoft.EntityFrameworkCore;
using TripGeniusBackend.Application.DTOs.AiChatResponse;
using TripGeniusBackend.Application.Interfaces.Queries;

namespace TripGeniusBackend.Infrastructure.Persistence.Queries;

public class AiChatQueryService : IAiChatQueryService
{
    private readonly AppDbContext _context;
    
    public AiChatQueryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<AiChatResponse>> GetUserHistory(int userId)
    {
        return await _context.AiChatHistories.Where(a => a.UserId == userId)
            .OrderBy(ah => ah.SentAt).Select(ah => new AiChatResponse{Message = ah.Content, Role = ah.Role}).ToListAsync();
    }

    public async Task<List<AiChatResponse>> GetShortTermMemory(int userId)
    {
        return await _context.AiChatHistories.Where(a => a.UserId == userId).OrderByDescending(a => a.SentAt).Take(10).OrderBy(a => a.SentAt) .Select(ah => new AiChatResponse{Message = ah.Content, Role = ah.Role}).ToListAsync();
    }
}