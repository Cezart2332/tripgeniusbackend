using Microsoft.AspNetCore.Mvc;
using TripGeniusBackend.API.DTOs;
using TripGeniusBackend.Application.DTOs.OffroadTrip;
using TripGeniusBackend.Application.DTOs.Trip;
using TripGeniusBackend.Application.Interfaces.Services;

namespace TripGeniusBackend.API.Helpers;

public static class TextModeration
{
    public static async Task<IActionResult?> ValidateFieldsAsync(
        IContentModerationService moderation,
        IEnumerable<(string Field, string? Value)> fields,
        CancellationToken cancellationToken = default)
    {
        foreach (var (field, value) in fields)
        {
            if (string.IsNullOrWhiteSpace(value))
                continue;

            var result = await moderation.CheckTextAsync(value.Trim(), cancellationToken);
            if (result.IsBlocked)
            {
                return new BadRequestObjectResult(new
                {
                    message = result.Reason ?? "Content was flagged as inappropriate.",
                    field,
                });
            }
        }

        return null;
    }

    public static IEnumerable<(string Field, string? Value)> CollectTripCreate(InitialTripRequest request)
    {
        foreach (var field in CollectTripCore(request.Title, request.Description, request.Tags))
            yield return field;

        if (request.Timelines is null)
            yield break;

        for (var i = 0; i < request.Timelines.Count; i++)
        {
            foreach (var field in CollectTimeline(request.Timelines[i], $"timelines[{i}]"))
                yield return field;
        }
    }

    public static IEnumerable<(string Field, string? Value)> CollectTripUpdate(
        InitialTripUpdateRequest request)
    {
        foreach (var field in CollectTripCore(request.Title, request.Description, request.Tags))
            yield return field;
    }

    public static IEnumerable<(string Field, string? Value)> CollectTimeline(
        UpdateTimelineRequest request)
    {
        foreach (var field in CollectTimelineFields(
                 "timeline",
                 request.StartingPoint,
                 request.EndPoint,
                 request.Note,
                 activities: null))
            yield return field;
    }

    public static IEnumerable<(string Field, string? Value)> CollectOffroadCreate(
        InitialOffroadTripRequest request)
    {
        foreach (var field in CollectTripCore(request.Title, request.Description, request.Tags))
            yield return field;

        if (request.Routes is null)
            yield break;

        for (var i = 0; i < request.Routes.Count; i++)
        {
            foreach (var field in CollectRoute(request.Routes[i], $"routes[{i}]"))
                yield return field;
        }
    }

    public static IEnumerable<(string Field, string? Value)> CollectOffroadUpdate(
        InitialOffroadTripUpdateRequest request)
    {
        foreach (var field in CollectTripCore(request.Title, request.Description, request.Tags))
            yield return field;
    }

    public static IEnumerable<(string Field, string? Value)> CollectRoute(
        UpdateOffroadRouteRequest request)
    {
        foreach (var field in CollectRoute(request.Name, request.Note, "route"))
            yield return field;
    }

    public static IEnumerable<(string Field, string? Value)> CollectRouteGpxForm(
        string name,
        string note)
    {
        foreach (var field in CollectRoute(name, note, "route"))
            yield return field;
    }

    public static IEnumerable<(string Field, string? Value)> CollectProfileUpdate(
        InitialUpdateRequest request)
    {
        yield return ("username", request.Username);
        yield return ("description", request.Description);

        if (request.Tags is null)
            yield break;

        for (var i = 0; i < request.Tags.Count; i++)
            yield return ($"tags[{i}]", request.Tags[i]);
    }

    private static IEnumerable<(string Field, string? Value)> CollectTripCore(
        string title,
        string description,
        IEnumerable<string>? tags)
    {
        yield return ("title", title);
        yield return ("description", description);

        if (tags is null)
            yield break;

        var index = 0;
        foreach (var tag in tags)
        {
            yield return ($"tags[{index}]", tag);
            index++;
        }
    }

    private static IEnumerable<(string Field, string? Value)> CollectTimeline(
        TripTimelineRequest timeline,
        string prefix)
    {
        foreach (var field in CollectTimelineFields(
                 prefix,
                 timeline.StartingPoint,
                 timeline.EndPoint,
                 timeline.Note,
                 timeline.Activities))
            yield return field;
    }

    private static IEnumerable<(string Field, string? Value)> CollectTimelineFields(
        string prefix,
        string startingPoint,
        string endPoint,
        string? note,
        List<TripActivityRequest>? activities)
    {
        yield return ($"{prefix}.startingPoint", startingPoint);
        yield return ($"{prefix}.endPoint", endPoint);
        yield return ($"{prefix}.note", note);

        if (activities is null)
            yield break;

        for (var i = 0; i < activities.Count; i++)
        {
            var activity = activities[i];
            yield return ($"{prefix}.activities[{i}].name", activity.Name);
            yield return ($"{prefix}.activities[{i}].description", activity.Description);
            yield return ($"{prefix}.activities[{i}].link", activity.Link);
        }
    }

    private static IEnumerable<(string Field, string? Value)> CollectRoute(
        OffroadRouteRequest route,
        string prefix)
    {
        foreach (var field in CollectRoute(route.Name, route.Note, prefix))
            yield return field;
    }

    private static IEnumerable<(string Field, string? Value)> CollectRoute(
        string name,
        string note,
        string prefix)
    {
        yield return ($"{prefix}.name", name);
        yield return ($"{prefix}.note", note);
    }
}
