using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripGeniusBackend.Application.Interfaces.UseCases;

namespace TripGeniusBackend.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AiController : ControllerBase
{
    private readonly IAiChatService _aiChatService;

    public AiController(IAiChatService aiChatService)
    {
        _aiChatService = aiChatService;   
    }
    
    [Authorize]
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
    {
        var messages = await _aiChatService.GetMessages();
        return Ok(messages);
    }
}