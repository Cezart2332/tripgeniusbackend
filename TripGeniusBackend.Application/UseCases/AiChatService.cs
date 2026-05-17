using TripGeniusBackend.Application.DTOs.AiChatResponse;
using TripGeniusBackend.Application.Interfaces;
using TripGeniusBackend.Application.Interfaces.Queries;
using TripGeniusBackend.Application.Interfaces.UseCases;

namespace TripGeniusBackend.Application.UseCases;

public class AiChatService : IAiChatService
{
    private readonly IJwtService _jwtService;
    private readonly IAiChatQueryService _aiChatQueryService;

    public AiChatService(IJwtService jwtService, IAiChatQueryService aiChatQueryService)
    {
        _jwtService = jwtService;
        _aiChatQueryService = aiChatQueryService;
    }

    public async Task<List<AiChatResponse>> GetMessages()
    {
        int userId = _jwtService.GetUserId();
        if (userId <= 0 || userId == null) throw new KeyNotFoundException("Not a valid user!");
        return await _aiChatQueryService.GetUserHistory(userId);
    }
}