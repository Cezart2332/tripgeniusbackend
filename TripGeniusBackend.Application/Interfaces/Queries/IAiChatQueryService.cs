using TripGeniusBackend.Application.DTOs.AiChatResponse;

namespace TripGeniusBackend.Application.Interfaces.Queries;

public interface IAiChatQueryService
{
    public Task<List<AiChatResponse>> GetUserHistory(int userId);
    public Task<List<AiChatResponse>> GetShortTermMemory(int userId);
}