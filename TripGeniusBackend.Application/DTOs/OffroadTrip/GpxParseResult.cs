namespace TripGeniusBackend.Application.DTOs.OffroadTrip;

public class GpxParseResult
{
    public string TrackGeoJson { get; set; } = string.Empty;
    public string? OriginalGpx { get; set; }
    public double DistanceMeters { get; set; }
    public double ElevationGainMeters { get; set; }
}
