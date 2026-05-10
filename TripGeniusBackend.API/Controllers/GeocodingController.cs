using Microsoft.AspNetCore.Mvc;
using TripGeniusBackend.Infrastructure.Persistence.Services;

namespace TripGeniusBackend.API.Controllers;


[ApiController]
[Route("api/[controller]")]
public class GeocodingController : ControllerBase
{
    private readonly GeocodingService _geocodingService;

    public GeocodingController(GeocodingService geocodingService)
    {
        _geocodingService = geocodingService;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string query, [FromQuery] int limit = 6)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest("Query is required.");

        var results = await _geocodingService.SearchAsync(query, limit);
        return Ok(results);
    }
}