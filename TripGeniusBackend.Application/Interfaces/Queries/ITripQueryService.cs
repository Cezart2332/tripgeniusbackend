using TripGeniusBackend.Application.DTOs.Trip;
using TripGeniusBackend.Domain.Entities;

namespace TripGeniusBackend.Application.Interfaces.Queries;

public interface ITripQueryService
{
    public Task<TripResponse?> GetTrip(int id,int userId);
    public Task<List<TripResponse>> GetTrips(int userId);
    public Task<List<TripResponse>> GetTripsForUser(int userId, TripsRequest tripsRequest);
    
    public Task<TripMember> GetMember(int tripId, int userId);
    public Task<TripTimelineResponse> GetTimeline(int tripTimelineId);
}