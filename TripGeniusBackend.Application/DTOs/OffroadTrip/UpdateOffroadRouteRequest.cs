using TripGeniusBackend.Domain.Enums;

namespace TripGeniusBackend.Application.DTOs.OffroadTrip;

public class UpdateOffroadRouteRequest
{
    public int Id { get; set; }
    public int TripId { get; set; }
    public int StartDay { get; set; }
    public int EndDay { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public string TrackGeoJson { get; set; } = string.Empty;
    public RouteSource Source { get; set; } = RouteSource.Drawn;
    public double DistanceMeters { get; set; }
    public double ElevationGainMeters { get; set; }
}
