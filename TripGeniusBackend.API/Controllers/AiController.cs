using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripGeniusBackend.Application.DTOs.AiChatResponse;
using TripGeniusBackend.Application.Interfaces;
using TripGeniusBackend.Application.Interfaces.UseCases;

namespace TripGeniusBackend.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AiController : ControllerBase
{
    private readonly IAiChatService _aiChatService;
    private readonly IAiService _aiService;

    public AiController(IAiChatService aiChatService, IAiService aiService)
    {
        _aiChatService = aiChatService;
        _aiService = aiService;
    }
    
    [Authorize]
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
    {
        var messages = await _aiChatService.GetMessages();
        return Ok(messages);
    }

    [Authorize]
    [HttpPost("generate-trip")]
    public async Task<IActionResult> GenerateTrip(AiTripPlanner aiTripPlanner)
    {

        var tripId = await _aiService.GenerateTripAsync(aiTripPlanner);
        return Ok(new { tripId, message = "Trip generated successfully" });
    }

    [Authorize]
    [HttpPost("generate-offroad-trip")]
    public async Task<IActionResult> GenerateOffroadTrip(AiOffroadTripPlanner planner)
    {
        var tripId = await _aiService.GenerateOffroadTripAsync(planner);
        return Ok(new { tripId, message = "Offroad trip generated successfully" });
    }
}