using TripGeniusBackend.Application.DTOs.Trip;

namespace TripGeniusBackend.Application.Interfaces;

public interface ITripService
{
    public Task CreateTrip(TripRequest tripRequest);
    public Task <List<TripResponse>> GetTripsForUser(TripsRequest tripsRequest);
    public Task<TripResponse> GetTrip(int id);
    public Task <List<TripResponse>> GetUserTrips();
}