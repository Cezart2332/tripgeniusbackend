using TripGeniusBackend.Application.DTOs.Trip;

namespace TripGeniusBackend.Application.Interfaces.Queries;

public interface ITripQueryService
{
    public Task<TripResponse?> GetTripById(int id,int userId);
    public Task<List<TripResponse>> GetTrips(int userId);
}