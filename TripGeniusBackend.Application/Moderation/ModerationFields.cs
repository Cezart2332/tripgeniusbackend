using TripGeniusBackend.Application.DTOs.OffroadTrip;
using TripGeniusBackend.Application.DTOs.Trip;

namespace TripGeniusBackend.Application.Moderation;

public static class ModerationFields
{
    public static IEnumerable<(string Field, string? Value)> FromTripRequest(TripRequest request)
    {
        foreach (var field in TripCore(request.Title, request.Description, request.Tags))
            yield return field;

        for (var i = 0; i < request.Timelines.Count; i++)
        {
            var timeline = request.Timelines[i];
            var prefix = $"timelines[{i}]";
            foreach (var field in TimelineFields(
                prefix,
                timeline.StartingPoint,
                timeline.EndPoint,
                timeline.Note,
                timeline.Activities))
                yield return field;
        }
    }

    public static IEnumerable<(string Field, string? Value)> FromTripUpdate(UpdateTripRequest request) =>
        TripCore(request.Title, request.Description, request.Tags);

    public static IEnumerable<(string Field, string? Value)> FromTimeline(UpdateTimelineRequest request) =>
        TimelineFields("timeline", request.StartingPoint, request.EndPoint, request.Note, null);

    public static IEnumerable<(string Field, string? Value)> FromOffroadTripRequest(OffroadTripRequest request)
    {
        foreach (var field in TripCore(request.Title, request.Description, request.Tags))
            yield return field;

        for (var i = 0; i < request.Routes.Count; i++)
        {
            foreach (var field in Route(request.Routes[i].Name, request.Routes[i].Note, $"routes[{i}]"))
                yield return field;
        }
    }

    public static IEnumerable<(string Field, string? Value)> FromOffroadTripUpdate(UpdateOffroadTripRequest request) =>
        TripCore(request.Title, request.Description, request.Tags);

    public static IEnumerable<(string Field, string? Value)> FromOffroadRoute(UpdateOffroadRouteRequest request) =>
        Route(request.Name, request.Note, "route");

    public static IEnumerable<(string Field, string? Value)> FromRouteGpxForm(string name, string note) =>
        Route(name, note, "route");

    public static IEnumerable<(string Field, string? Value)> FromProfileUpdate(
        string username,
        string description,
        List<string>? tags)
    {
        yield return ("username", username);
        yield return ("description", description);
        if (tags is null)
            yield break;
        for (var i = 0; i < tags.Count; i++)
            yield return ($"tags[{i}]", tags[i]);
    }

    private static IEnumerable<(string Field, string? Value)> TripCore(
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

    private static IEnumerable<(string Field, string? Value)> TimelineFields(
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

    private static IEnumerable<(string Field, string? Value)> Route(string name, string note, string prefix)
    {
        yield return ($"{prefix}.name", name);
        yield return ($"{prefix}.note", note);
    }

    public static List<(string Field, string Value)> ToReviewList(
        IEnumerable<(string Field, string? Value)> fields) =>
        fields
            .Where(f => !string.IsNullOrWhiteSpace(f.Value))
            .Select(f => (f.Field, f.Value!.Trim()))
            .ToList();
}
