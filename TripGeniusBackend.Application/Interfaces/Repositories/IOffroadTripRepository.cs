using TripGeniusBackend.Domain.Entities;

namespace TripGeniusBackend.Application.Interfaces.Repositories;

public interface IOffroadTripRepository
{
    Task CreateTrip(OffroadTrip trip);
    Task UpdateTrip(OffroadTrip trip);
    Task<OffroadTrip?> GetTripById(int id);
    Task SaveChanges();
    Task<List<OffroadTrip>> SearchSimilarAsync(float[] queryEmbedding, int userId, int limit = 5);
}
