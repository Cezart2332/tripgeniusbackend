using TripGeniusBackend.Application.DTOs.Trip;

namespace TripGeniusBackend.Application.Interfaces.Queries;

public interface IMessageQueryService
{
    public Task<List<MessageResponse>> GetMessages(int tripId); 
}