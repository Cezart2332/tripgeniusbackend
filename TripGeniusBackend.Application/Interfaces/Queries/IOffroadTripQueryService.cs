using TripGeniusBackend.Application.DTOs.OffroadTrip;

namespace TripGeniusBackend.Application.Interfaces.Queries;

public interface IOffroadTripQueryService
{
    Task<OffroadTripResponse?> GetTrip(int id, int userId);
    Task<List<OffroadTripResponse>> GetTrips(int userId);
    Task<List<OffroadTripResponse>> GetTripsForUser(int userId, OffroadTripsRequest request);
    Task<OffroadRouteResponse?> GetRoute(int routeId);
}
