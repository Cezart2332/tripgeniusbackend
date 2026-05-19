using TripGeniusBackend.Application.DTOs.Trip;

namespace TripGeniusBackend.Application.Interfaces.Queries;

public interface IOffroadMessageQueryService
{
    Task<List<MessageResponse>> GetMessages(int offroadTripId);
}
