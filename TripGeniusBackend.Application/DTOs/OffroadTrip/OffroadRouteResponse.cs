namespace TripGeniusBackend.Application.DTOs.OffroadTrip;

public class OffroadRouteResponse
{
    public int Id { get; set; }
    public int StartDay { get; set; }
    public int EndDay { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public string TrackGeoJson { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public double DistanceMeters { get; set; }
    public double ElevationGainMeters { get; set; }
}
