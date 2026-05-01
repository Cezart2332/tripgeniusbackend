using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using TripGeniusBackend.Application.Interfaces.Repositories;
using TripGeniusBackend.Domain.Entities;

namespace TripGeniusBackend.Infrastructure.Persistence.Repositories;

public class AiMemoryRepository : IAiMemoryRepository
{
    private readonly AppDbContext _context;

    public AiMemoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<AiMemory>> SearchSimilarAsync(float[] queryEmbedding, int userId, int limit = 5)
    {
        var vector = new Vector(queryEmbedding);
        return await _context.AiMemories.Where(am => am.UserId == userId)
            .OrderBy(am => am.Embedding.CosineDistance(vector)).Take(limit).ToListAsync();
    }

    public Task Create(AiMemory aiMemory)
    {
        _context.AiMemories.Add(aiMemory);
        return Task.CompletedTask;
    }
    public async Task SaveChanges()
    {
        await _context.SaveChangesAsync();
    }
}