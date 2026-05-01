using TripGeniusBackend.Domain.Entities;

namespace TripGeniusBackend.Application.Interfaces.Repositories;

public interface IAiMemoryRepository
{
    public Task<List<AiMemory>> SearchSimilarAsync(float[] queryEmbedding, int userId, int limit = 5);
    public Task Create(AiMemory aiMemory);
    public Task SaveChanges();
}