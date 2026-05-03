using TripGeniusBackend.Application.DTOs.Trip;
using TripGeniusBackend.Domain.Enums;

namespace TripGeniusBackend.Application.Interfaces.UseCases;

public interface ITripService
{
    public Task CreateTrip(TripRequest tripRequest);
    public Task <List<TripResponse>> GetTripsForUser(TripsRequest tripsRequest);
    public Task<List<TripResponse>> GetTrips();
    public Task<TripResponse> GetTrip(int id);
    
    public Task MembershipRequest(int tripId, int invitedId);
    public Task MembershipResponse(int tripId, int invitedId, string status, string action);
    public Task RemoveMember(int tripId, int removedId);
    public Task UpdateMember(UpdateRoleRequest updateRoleRequest);
    public Task<TripResponse> UpdateTrip(UpdateTripRequest updateTripRequest);
    
    public Task<TripTimelineResponse> GetTimeline(int tripId, int id);
    public Task<TripTimelineResponse> UpdateTimeline(UpdateTimelineRequest updateTimelineRequest);
    public Task RemoveTimeline(int id, int tripId);
    public Task AddTimeline(UpdateTimelineRequest updateTimelineRequest);
    public Task<List<MessageResponse>> GetMessages(int tripId);

    

}