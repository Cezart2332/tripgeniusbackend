using TripGeniusBackend.Application.DTOs.Trip;

namespace TripGeniusBackend.Application.Interfaces;

public interface ITripService
{
    public Task CreateTrip(TripRequest tripRequest);
    public Task<List<TripResponse>> GetTrips();
    public Task<TripResponse> GetTrip(int id);
}