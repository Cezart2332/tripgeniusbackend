using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using TripGeniusBackend.Application.DTOs.OffroadTrip;
using TripGeniusBackend.Application.Interfaces.Services;
using TripGeniusBackend.Domain.Entities;

namespace TripGeniusBackend.Infrastructure.Persistence.Services;

public class GpxService : IGpxService
{
    private const int MaxPoints = 5000;
    private static readonly XNamespace GpxNs = "http://www.topografix.com/GPX/1/1";

    public async Task<GpxParseResult> ParseGpxAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        var xml = await reader.ReadToEndAsync(cancellationToken);
        var doc = XDocument.Parse(xml);

        var points = doc.Descendants()
            .Where(e => e.Name.LocalName == "trkpt")
            .Select(ParseTrackPoint)
            .Where(p => p != null)
            .Select(p => p!)
            .ToList();

        if (points.Count < 2)
            throw new ArgumentException("GPX must contain at least two track points.");

        points = SimplifyPoints(points, MaxPoints);
        var distance = ComputeDistanceMeters(points);
        var elevationGain = ComputeElevationGain(points);
        var geoJson = BuildLineStringGeoJson(points);

        return new GpxParseResult
        {
            TrackGeoJson = geoJson,
            OriginalGpx = xml,
            DistanceMeters = distance,
            ElevationGainMeters = elevationGain
        };
    }

    public byte[] BuildRouteGpx(OffroadRoute route, string tripTitle)
    {
        var coords = ExtractCoordinates(route.TrackGeoJson);
        return Encoding.UTF8.GetBytes(BuildGpxDocument(tripTitle, route.Name, coords));
    }

    public byte[] BuildTripGpx(OffroadTrip trip)
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.Append("<gpx version=\"1.1\" creator=\"TripGenius\" xmlns=\"http://www.topografix.com/GPX/1/1\">");
        sb.Append($"<metadata><name>{EscapeXml(trip.Title)}</name></metadata>");

        foreach (var route in trip.Routes)
        {
            var coords = ExtractCoordinates(route.TrackGeoJson);
            sb.Append($"<trk><name>{EscapeXml(route.Name)}</name><trkseg>");
            foreach (var (lng, lat, ele) in coords)
            {
                sb.Append($"<trkpt lat=\"{lat.ToString(CultureInfo.InvariantCulture)}\" lon=\"{lng.ToString(CultureInfo.InvariantCulture)}\">");
                if (ele.HasValue)
                    sb.Append($"<ele>{ele.Value.ToString(CultureInfo.InvariantCulture)}</ele>");
                sb.Append("</trkpt>");
            }
            sb.Append("</trkseg></trk>");
        }

        sb.Append("</gpx>");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static TrackPoint? ParseTrackPoint(XElement trkpt)
    {
        var latAttr = trkpt.Attribute("lat")?.Value;
        var lonAttr = trkpt.Attribute("lon")?.Value;
        if (!double.TryParse(latAttr, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat)) return null;
        if (!double.TryParse(lonAttr, NumberStyles.Float, CultureInfo.InvariantCulture, out var lng)) return null;
        if (lat < -90 || lat > 90 || lng < -180 || lng > 180) return null;

        double? ele = null;
        var eleEl = trkpt.Elements().FirstOrDefault(e => e.Name.LocalName == "ele");
        if (eleEl != null && double.TryParse(eleEl.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var e))
            ele = e;

        return new TrackPoint(lng, lat, ele);
    }

    private static string BuildLineStringGeoJson(List<TrackPoint> points)
    {
        var coordinates = points.Select(p =>
            p.Elevation.HasValue
                ? new object[] { p.Lng, p.Lat, p.Elevation.Value }
                : new object[] { p.Lng, p.Lat }).ToArray();
        var geoJson = new { type = "LineString", coordinates };
        return JsonSerializer.Serialize(geoJson);
    }

    private static List<(double Lng, double Lat, double? Ele)> ExtractCoordinates(string trackGeoJson)
    {
        using var doc = JsonDocument.Parse(trackGeoJson);
        var root = doc.RootElement;
        if (!root.TryGetProperty("coordinates", out var coords) || coords.ValueKind != JsonValueKind.Array)
            throw new ArgumentException("Invalid track GeoJSON.");

        var result = new List<(double, double, double?)>();
        foreach (var point in coords.EnumerateArray())
        {
            if (point.GetArrayLength() < 2) continue;
            var lng = point[0].GetDouble();
            var lat = point[1].GetDouble();
            double? ele = point.GetArrayLength() > 2 ? point[2].GetDouble() : null;
            result.Add((lng, lat, ele));
        }

        if (result.Count < 2) throw new ArgumentException("Route must have at least two coordinates.");
        return result;
    }

    private static string BuildGpxDocument(string tripTitle, string routeName, List<(double Lng, double Lat, double? Ele)> coords)
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.Append("<gpx version=\"1.1\" creator=\"TripGenius\" xmlns=\"http://www.topografix.com/GPX/1/1\">");
        sb.Append($"<metadata><name>{EscapeXml(tripTitle)}</name></metadata>");
        sb.Append($"<trk><name>{EscapeXml(routeName)}</name><trkseg>");
        foreach (var (lng, lat, ele) in coords)
        {
            sb.Append($"<trkpt lat=\"{lat.ToString(CultureInfo.InvariantCulture)}\" lon=\"{lng.ToString(CultureInfo.InvariantCulture)}\">");
            if (ele.HasValue)
                sb.Append($"<ele>{ele.Value.ToString(CultureInfo.InvariantCulture)}</ele>");
            sb.Append("</trkpt>");
        }
        sb.Append("</trkseg></trk></gpx>");
        return sb.ToString();
    }

    private static string EscapeXml(string value) =>
        value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    private static double ComputeDistanceMeters(List<TrackPoint> points)
    {
        double total = 0;
        for (var i = 1; i < points.Count; i++)
            total += HaversineMeters(points[i - 1].Lat, points[i - 1].Lng, points[i].Lat, points[i].Lng);
        return total;
    }

    private static double ComputeElevationGain(List<TrackPoint> points)
    {
        double gain = 0;
        double? prev = null;
        foreach (var p in points)
        {
            if (!p.Elevation.HasValue) continue;
            if (prev.HasValue && p.Elevation > prev)
                gain += p.Elevation.Value - prev.Value;
            prev = p.Elevation;
        }
        return gain;
    }

    private static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double r = 6371000;
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return 2 * r * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double DegreesToRadians(double deg) => deg * Math.PI / 180;

    private static List<TrackPoint> SimplifyPoints(List<TrackPoint> points, int maxPoints)
    {
        if (points.Count <= maxPoints) return points;
        var step = (int)Math.Ceiling((double)points.Count / maxPoints);
        var simplified = new List<TrackPoint>();
        for (var i = 0; i < points.Count; i += step)
            simplified.Add(points[i]);
        if (simplified[^1] != points[^1])
            simplified.Add(points[^1]);
        return simplified;
    }

    private sealed record TrackPoint(double Lng, double Lat, double? Elevation);
}
