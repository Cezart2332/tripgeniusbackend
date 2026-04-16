using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripGeniusBackend.API.DTOs;
using TripGeniusBackend.Application.DTOs.Trip;
using TripGeniusBackend.Application.Interfaces;

namespace TripGeniusBackend.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TripController : ControllerBase
{
    private readonly ITripService _tripService;

    public TripController(ITripService tripService)
    {
        _tripService = tripService;
    }

    [Authorize]
    [HttpPost("create-trip")]
    public async Task<IActionResult> CreateTrip([FromForm] InitialTripRequest initialTripRequest)
    {
        try
        {
            
            var tripRequest = new TripRequest
            {
                Title = initialTripRequest.Title,
                Description = initialTripRequest.Description,
                ImageStream = initialTripRequest.Image?.OpenReadStream(),
                ImageFileName = initialTripRequest.Image?.FileName,
                StartingDate = initialTripRequest.StartingDate,
                EndingDate = initialTripRequest.EndingDate,
                Status = initialTripRequest.Status,
                Tags = initialTripRequest.Tags,
                MaxParticipants = initialTripRequest.MaxParticipants,
                Price = initialTripRequest.Price,
                Timelines = initialTripRequest.Timelines
            };
            await _tripService.CreateTrip(tripRequest);
            return Ok();
        }
        catch (ArgumentException e)
        {
            return BadRequest(new { message = e.Message });
        }
    }
}