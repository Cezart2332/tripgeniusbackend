using TripGeniusBackend.Application.DTOs.AiChatResponse;

namespace TripGeniusBackend.Application.Interfaces.UseCases;

public interface IAiChatService
{
    public Task<List<AiChatResponse>> GetMessages();
}