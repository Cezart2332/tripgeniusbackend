using TripGeniusBackend.Domain.Entities;

namespace TripGeniusBackend.Application.Interfaces;

public interface ITripRepository
{
    public Task CreateTrip(Trip trip);
    public Task UpdateTrip(Trip trip);
    public Task SaveChanges();
    public Task<Trip?> GetTripById(int id);
    public Task<List<Trip>> GetTrips();
    
}