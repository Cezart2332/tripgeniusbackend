using System.Text.Json;

namespace TripGeniusBackend.Application.Helpers;

/// <summary>
/// PostgreSQL jsonb columns reject empty strings; offroad routes may be created before GPX is uploaded.
/// </summary>
public static class OffroadRouteGeoJson
{
    public const string EmptyLineString = """{"type":"LineString","coordinates":[]}""";

    public static string NormalizeForStorage(string? trackGeoJson)
    {
        if (string.IsNullOrWhiteSpace(trackGeoJson))
            return EmptyLineString;

        var trimmed = trackGeoJson.Trim();
        try
        {
            using var doc = JsonDocument.Parse(trimmed);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                throw new ArgumentException("TrackGeoJson must be a JSON object.");
            return trimmed;
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("TrackGeoJson must be valid JSON.", ex);
        }
    }
}
