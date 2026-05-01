using TripGeniusBackend.Application.Interfaces.Repositories;
using TripGeniusBackend.Domain.Entities;

namespace TripGeniusBackend.Infrastructure.Persistence.Repositories;

public class AiChatRepository : IAiChatRepository
{
    private readonly AppDbContext _context;
    
    public AiChatRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task Create(AiChatHistory aiChatHistory)
    {
        _context.AiChatHistories.Add(aiChatHistory);
        return Task.CompletedTask;
    }

    public async Task SaveChanges()
    {
        await _context.SaveChangesAsync();
    }
    
}