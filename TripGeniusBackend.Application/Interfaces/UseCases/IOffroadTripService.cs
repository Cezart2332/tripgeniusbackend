using TripGeniusBackend.Application.DTOs.OffroadTrip;
using TripGeniusBackend.Application.DTOs.Trip;

namespace TripGeniusBackend.Application.Interfaces.UseCases;

public interface IOffroadTripService
{
    Task<int> CreateTrip(OffroadTripRequest request);
    Task<List<OffroadTripResponse>> GetTripsForUser(OffroadTripsRequest request);
    Task<List<OffroadTripResponse>> GetTrips();
    Task<OffroadTripResponse> GetTrip(int id);
    Task<OffroadTripResponse> UpdateTrip(UpdateOffroadTripRequest request);
    Task MembershipRequest(int tripId, int invitedId);
    Task MembershipResponse(int tripId, int invitedId, string status, string action);
    Task RemoveMember(int tripId, int removedId);
    Task UpdateMember(UpdateRoleRequest updateRoleRequest);
    Task<OffroadRouteResponse> GetRoute(int tripId, int routeId);
    Task<OffroadRouteResponse> AddRoute(UpdateOffroadRouteRequest request, Stream? gpxStream = null);
    Task<OffroadRouteResponse> UpdateRoute(UpdateOffroadRouteRequest request, Stream? gpxStream = null);
    Task RemoveRoute(int tripId, int routeId);
    Task<byte[]> ExportRouteGpx(int tripId, int routeId);
    Task<byte[]> ExportTripGpx(int tripId);
    Task<List<MessageResponse>> GetMessages(int tripId);
}
