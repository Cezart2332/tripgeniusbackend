using TripGeniusBackend.Domain.Entities;

namespace TripGeniusBackend.Application.Interfaces.Repositories;

public interface ITripRepository
{
    public Task CreateTrip(Trip trip);
    public Task UpdateTrip(Trip trip);
    
    public Task<Trip?> GetTripById(int id);
    public Task<List<Trip>> SearchSimilarAsync(float[] queryEmbedding,int userId, int limit = 5);
    public Task SaveChanges();
    
    
}