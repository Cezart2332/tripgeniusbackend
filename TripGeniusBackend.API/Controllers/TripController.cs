using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripGeniusBackend.API.DTOs;
using TripGeniusBackend.API.Helpers;
using TripGeniusBackend.Application.DTOs.Trip;
using TripGeniusBackend.Application.Interfaces.Services;
using TripGeniusBackend.Application.Interfaces.UseCases;

namespace TripGeniusBackend.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TripController : ControllerBase
{
    private readonly ITripService _tripService;
    private readonly IContentModerationService _moderation;

    public TripController(ITripService tripService, IContentModerationService moderation)
    {
        _tripService = tripService;
        _moderation = moderation;
    }

    [Authorize]
    [HttpPost("create-trip")]
    public async Task<IActionResult> CreateTrip([FromForm] InitialTripRequest initialTripRequest)
    {
        try
        {
            var (imageStream, imageRejection) = await ImageModeration.ValidateUploadAsync(
                initialTripRequest.Image, _moderation);
            if (imageRejection != null)
                return imageRejection;

            var textRejection = await TextModeration.ValidateFieldsAsync(
                _moderation,
                TextModeration.CollectTripCreate(initialTripRequest));
            if (textRejection != null)
                return textRejection;

            try
            {
                var tripRequest = new TripRequest
                {
                    Title = initialTripRequest.Title,
                    Description = initialTripRequest.Description,
                    ImageStream = imageStream,
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
            finally
            {
                if (imageStream != null)
                    await imageStream.DisposeAsync();
            }
        }
        catch (ArgumentException e)
        {
            return BadRequest(new { message = e.Message });
        }
    }

    [Authorize]
    [HttpGet("get-all-trips")]
    public async Task<IActionResult> GetAllTrips()
    {
        var trips = await _tripService.GetTrips();
        return Ok(trips);
    }

    [Authorize]
    [HttpPost("get-trips")]
    public async Task<IActionResult> GetTrips([FromBody] TripsRequest tripsRequest)
    {
        var trips = await _tripService.GetTripsForUser(tripsRequest);
        return Ok(trips);
    }

    [Authorize]
    [HttpGet("get-trip/{tripId}")]
    public async Task<IActionResult> GetTrip(int tripId)
    {
        var trip = await _tripService.GetTrip(tripId);
        return Ok(trip);
    }
    

    [Authorize]
    [HttpPost("membership-request")]
    public async Task<IActionResult> MembershipRequest(MemberRequest memberRequest)
    {
        await _tripService.MembershipRequest(memberRequest.TripId, memberRequest.UserId);
        return Ok();
    }

    [Authorize]
    [HttpPatch("membership-response")]
    public async Task<IActionResult> MembershipResponse(MemberResponse memberResponse)
    {
        await _tripService.MembershipResponse(memberResponse.TripId, memberResponse.InvitedId, memberResponse.MemberStatus, memberResponse.Action);
        return Ok();
    }

    [Authorize]
    [HttpDelete("remove-member/{tripId}/{removedId}")]
    public async Task<IActionResult> RemoveMember(int tripId, int removedId)
    {
        await _tripService.RemoveMember(tripId, removedId);
        return Ok();
    }

    [Authorize]
    [HttpPatch("change-role")]
    public async Task<IActionResult> ChangeRole(UpdateRoleRequest updateRoleRequest)
    {
        await _tripService.UpdateMember(updateRoleRequest);
        return Ok();
    }
    [Authorize]
    [HttpPatch("update-trip")]
    public async Task<IActionResult> UpdateTrip([FromForm] InitialTripUpdateRequest initialTripUpdateRequest)
    {
        var (imageStream, imageRejection) = await ImageModeration.ValidateUploadAsync(
            initialTripUpdateRequest.Image, _moderation);
        if (imageRejection != null)
            return imageRejection;

        var textRejection = await TextModeration.ValidateFieldsAsync(
            _moderation,
            TextModeration.CollectTripUpdate(initialTripUpdateRequest));
        if (textRejection != null)
            return textRejection;

        try
        {
            var updateTripRequest = new UpdateTripRequest
            {
                Id = initialTripUpdateRequest.Id,
                Title = initialTripUpdateRequest.Title,
                Description = initialTripUpdateRequest.Description,
                ImageStream = imageStream,
                ImageFileName = initialTripUpdateRequest.Image?.FileName,
                StartingDate = initialTripUpdateRequest.StartingDate,
                EndingDate = initialTripUpdateRequest.EndingDate,
                Status = initialTripUpdateRequest.Status,
                Tags = initialTripUpdateRequest.Tags,
                MaxParticipants = initialTripUpdateRequest.MaxParticipants,
            };
            await _tripService.UpdateTrip(updateTripRequest);
            return Ok();
        }
        finally
        {
            if (imageStream != null)
                await imageStream.DisposeAsync();
        }
    }
    [Authorize]
    [HttpGet("timeline/{tripId}/{timelineId}")]
    public async Task<IActionResult> GetTimeline(int tripId, int timelineId)
    {
        var timeline = await _tripService.GetTimeline(tripId, timelineId);
        return Ok(timeline);
    }

    [Authorize]
    [HttpPatch("update-timeline")]
    public async Task<IActionResult> UpdateTimeline(UpdateTimelineRequest updateTimelineRequest)
    {
        var textRejection = await TextModeration.ValidateFieldsAsync(
            _moderation,
            TextModeration.CollectTimeline(updateTimelineRequest));
        if (textRejection != null)
            return textRejection;

        var timeline = await _tripService.UpdateTimeline(updateTimelineRequest);
        return Ok(timeline);
    }
    [Authorize]
    [HttpDelete("timeline-remove/{tripId}/{timelineId}")]
    public async Task<IActionResult> RemoveTimeline(int tripId, int timelineId)
    {
        await _tripService.RemoveTimeline(tripId, timelineId);
        return Ok();
    }

    [Authorize]
    [HttpPost("add-timeline")]
    public async Task<IActionResult> AddTimeline(UpdateTimelineRequest updateTimelineRequest)
    {
        var textRejection = await TextModeration.ValidateFieldsAsync(
            _moderation,
            TextModeration.CollectTimeline(updateTimelineRequest));
        if (textRejection != null)
            return textRejection;

        await _tripService.AddTimeline(updateTimelineRequest);
        return Ok();
    }
    [Authorize]
    [HttpGet("get-messages/{tripId}")]
    public async Task<IActionResult> GetMessages(int tripId)
    {
        var messages = await _tripService.GetMessages(tripId);
        return Ok(messages);
    }

    [Authorize]
    [HttpGet("export-costs/{tripId}")]
    public async Task<IActionResult> ExportCosts(int tripId)
    {
        var pdf = await _tripService.ExportCosts(tripId);
        return File(pdf, "application/pdf", $"TripCosts_{tripId}.pdf");
    }
}