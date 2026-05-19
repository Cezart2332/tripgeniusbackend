using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripGeniusBackend.Application.DTOs.Geocoding;
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

    [Authorize]
    [HttpPost("elevation")]
    public async Task<IActionResult> LookupElevation([FromBody] ElevationLookupRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Points == null || request.Points.Count < 2)
            return BadRequest(new { message = "At least two points are required." });

        foreach (var p in request.Points)
        {
            if (p.Lat is < -90 or > 90 || double.IsNaN(p.Lat) || double.IsInfinity(p.Lat))
                return BadRequest(new { message = "Invalid latitude." });
            if (p.Lng is < -180 or > 180 || double.IsNaN(p.Lng) || double.IsInfinity(p.Lng))
                return BadRequest(new { message = "Invalid longitude." });
        }

        try
        {
            var elevations = await _geocodingService.LookupElevationsAsync(request.Points, cancellationToken);
            return Ok(new ElevationLookupResponse { Elevations = elevations });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}