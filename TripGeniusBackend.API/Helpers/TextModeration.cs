using TripGeniusBackend.API.DTOs;
using TripGeniusBackend.Application.DTOs.OffroadTrip;
using TripGeniusBackend.Application.DTOs.Trip;
using TripGeniusBackend.Application.Moderation;

namespace TripGeniusBackend.API.Helpers;

/// <summary>Collects text fields for background moderation (API DTOs → application helpers).</summary>
public static class TextModeration
{
    public static IEnumerable<(string Field, string? Value)> CollectTripCreate(InitialTripRequest request) =>
        ModerationFields.FromTripRequest(new TripRequest
        {
            Title = request.Title,
            Description = request.Description,
            Tags = request.Tags,
            Timelines = request.Timelines ?? new List<TripTimelineRequest>(),
        });

    public static IEnumerable<(string Field, string? Value)> CollectTripUpdate(InitialTripUpdateRequest request) =>
        ModerationFields.FromTripUpdate(new UpdateTripRequest
        {
            Title = request.Title,
            Description = request.Description,
            Tags = request.Tags,
        });

    public static IEnumerable<(string Field, string? Value)> CollectTimeline(UpdateTimelineRequest timeline) =>
        ModerationFields.FromTimeline(timeline);

    public static IEnumerable<(string Field, string? Value)> CollectOffroadCreate(InitialOffroadTripRequest request) =>
        ModerationFields.FromOffroadTripRequest(new OffroadTripRequest
        {
            Title = request.Title,
            Description = request.Description,
            Tags = request.Tags,
            Routes = request.Routes ?? new List<OffroadRouteRequest>(),
        });

    public static IEnumerable<(string Field, string? Value)> CollectOffroadUpdate(
        InitialOffroadTripUpdateRequest request) =>
        ModerationFields.FromOffroadTripUpdate(new UpdateOffroadTripRequest
        {
            Title = request.Title,
            Description = request.Description,
            Tags = request.Tags,
        });

    public static IEnumerable<(string Field, string? Value)> CollectRoute(UpdateOffroadRouteRequest request) =>
        ModerationFields.FromOffroadRoute(request);

    public static IEnumerable<(string Field, string? Value)> CollectRouteGpxForm(string name, string note) =>
        ModerationFields.FromRouteGpxForm(name, note);

    public static IEnumerable<(string Field, string? Value)> CollectProfileUpdate(InitialUpdateRequest request) =>
        ModerationFields.FromProfileUpdate(request.Username, request.Description, request.Tags);
}
