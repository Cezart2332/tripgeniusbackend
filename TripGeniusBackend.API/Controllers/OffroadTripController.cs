using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripGeniusBackend.API.DTOs;
using TripGeniusBackend.Application.DTOs.OffroadTrip;
using TripGeniusBackend.Application.DTOs.Trip;
using TripGeniusBackend.Application.Interfaces.UseCases;

namespace TripGeniusBackend.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OffroadTripController : ControllerBase
{
    private readonly IOffroadTripService _offroadTripService;

    public OffroadTripController(IOffroadTripService offroadTripService) => _offroadTripService = offroadTripService;

    [Authorize]
    [HttpPost("create-offroad-trip")]
    public async Task<IActionResult> CreateTrip([FromForm] InitialOffroadTripRequest request)
    {
        try
        {
            await _offroadTripService.CreateTrip(new OffroadTripRequest
            {
                Title = request.Title,
                Description = request.Description,
                ImageStream = request.Image?.OpenReadStream(),
                ImageFileName = request.Image?.FileName,
                StartingDate = request.StartingDate,
                EndingDate = request.EndingDate,
                Status = request.Status,
                Tags = request.Tags,
                MaxParticipants = request.MaxParticipants,
                Price = request.Price,
                Routes = request.Routes ?? new List<OffroadRouteRequest>()
            });
            return Ok();
        }
        catch (ArgumentException e)
        {
            return BadRequest(new { message = e.Message });
        }
    }

    [Authorize]
    [HttpGet("get-all-offroad-trips")]
    public async Task<IActionResult> GetAllTrips() => Ok(await _offroadTripService.GetTrips());

    [Authorize]
    [HttpPost("get-offroad-trips")]
    public async Task<IActionResult> GetTrips([FromBody] OffroadTripsRequest request) =>
        Ok(await _offroadTripService.GetTripsForUser(request));

    [Authorize]
    [HttpGet("get-offroad-trip/{tripId}")]
    public async Task<IActionResult> GetTrip(int tripId) => Ok(await _offroadTripService.GetTrip(tripId));

    [Authorize]
    [HttpPatch("update-offroad-trip")]
    public async Task<IActionResult> UpdateTrip([FromForm] InitialOffroadTripUpdateRequest request)
    {
        var updated = await _offroadTripService.UpdateTrip(new UpdateOffroadTripRequest
        {
            Id = request.Id,
            Title = request.Title,
            Description = request.Description,
            ImageStream = request.Image?.OpenReadStream(),
            ImageFileName = request.Image?.FileName,
            StartingDate = request.StartingDate,
            EndingDate = request.EndingDate,
            Status = request.Status,
            Tags = request.Tags,
            MaxParticipants = request.MaxParticipants,
            Price = request.Price
        });
        return Ok(updated);
    }

    [Authorize]
    [HttpPost("add-route")]
    public async Task<IActionResult> AddRoute([FromBody] UpdateOffroadRouteRequest request) =>
        Ok(await _offroadTripService.AddRoute(request));

    [Authorize]
    [HttpPost("import-route-gpx")]
    public async Task<IActionResult> ImportRouteGpx([FromForm] int tripId, [FromForm] int startDay, [FromForm] int endDay,
        [FromForm] string name, [FromForm] string note, IFormFile gpx)
    {
        if (gpx == null || gpx.Length == 0) return BadRequest(new { message = "GPX file is required." });
        await using var stream = gpx.OpenReadStream();
        var route = await _offroadTripService.AddRoute(new UpdateOffroadRouteRequest
        {
            TripId = tripId,
            StartDay = startDay,
            EndDay = endDay,
            Name = name,
            Note = note
        }, stream);
        return Ok(route);
    }

    [Authorize]
    [HttpGet("route/{tripId}/{routeId}")]
    public async Task<IActionResult> GetRoute(int tripId, int routeId) =>
        Ok(await _offroadTripService.GetRoute(tripId, routeId));

    [Authorize]
    [HttpPatch("update-route")]
    public async Task<IActionResult> UpdateRoute([FromBody] UpdateOffroadRouteRequest request) =>
        Ok(await _offroadTripService.UpdateRoute(request));

    [Authorize]
    [HttpPatch("update-route-gpx")]
    public async Task<IActionResult> UpdateRouteGpx([FromForm] UpdateOffroadRouteRequest request, IFormFile gpx)
    {
        await using var stream = gpx.OpenReadStream();
        return Ok(await _offroadTripService.UpdateRoute(request, stream));
    }

    [Authorize]
    [HttpDelete("route-remove/{tripId}/{routeId}")]
    public async Task<IActionResult> RemoveRoute(int tripId, int routeId)
    {
        await _offroadTripService.RemoveRoute(tripId, routeId);
        return Ok();
    }

    [Authorize]
    [HttpGet("export-route-gpx/{tripId}/{routeId}")]
    public async Task<IActionResult> ExportRouteGpx(int tripId, int routeId)
    {
        var bytes = await _offroadTripService.ExportRouteGpx(tripId, routeId);
        return File(bytes, "application/gpx+xml", $"offroad-route-{routeId}.gpx");
    }

    [Authorize]
    [HttpGet("export-trip-gpx/{tripId}")]
    public async Task<IActionResult> ExportTripGpx(int tripId)
    {
        var bytes = await _offroadTripService.ExportTripGpx(tripId);
        return File(bytes, "application/gpx+xml", $"offroad-trip-{tripId}.gpx");
    }

    [Authorize]
    [HttpPost("membership-request")]
    public async Task<IActionResult> MembershipRequest(MemberRequest memberRequest)
    {
        await _offroadTripService.MembershipRequest(memberRequest.TripId, memberRequest.UserId);
        return Ok();
    }

    [Authorize]
    [HttpPatch("membership-response")]
    public async Task<IActionResult> MembershipResponse(MemberResponse memberResponse)
    {
        await _offroadTripService.MembershipResponse(memberResponse.TripId, memberResponse.InvitedId,
            memberResponse.MemberStatus, memberResponse.Action);
        return Ok();
    }

    [Authorize]
    [HttpDelete("remove-member/{tripId}/{removedId}")]
    public async Task<IActionResult> RemoveMember(int tripId, int removedId)
    {
        await _offroadTripService.RemoveMember(tripId, removedId);
        return Ok();
    }

    [Authorize]
    [HttpPatch("change-role")]
    public async Task<IActionResult> ChangeRole(UpdateRoleRequest updateRoleRequest)
    {
        await _offroadTripService.UpdateMember(updateRoleRequest);
        return Ok();
    }

    [Authorize]
    [HttpGet("get-messages/{tripId}")]
    public async Task<IActionResult> GetMessages(int tripId) => Ok(await _offroadTripService.GetMessages(tripId));
}
