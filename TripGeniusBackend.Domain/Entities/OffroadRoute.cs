using TripGeniusBackend.Domain.Enums;

namespace TripGeniusBackend.Domain.Entities;

public class OffroadRoute
{
    public int Id { get; private set; }
    public int OffroadTripId { get; private set; }
    public OffroadTrip OffroadTrip { get; private set; } = null!;
    public int StartDay { get; private set; }
    public int EndDay { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Note { get; private set; } = string.Empty;
    public string TrackGeoJson { get; private set; } = string.Empty;
    public string? OriginalGpx { get; private set; }
    public RouteSource Source { get; private set; }
    public double DistanceMeters { get; private set; }
    public double ElevationGainMeters { get; private set; }

    protected OffroadRoute() { }

    public OffroadRoute(
        int startDay,
        int endDay,
        string name,
        string note,
        string trackGeoJson,
        RouteSource source,
        double distanceMeters,
        double elevationGainMeters,
        string? originalGpx = null)
    {
        StartDay = startDay;
        EndDay = endDay;
        Name = name;
        Note = note;
        TrackGeoJson = trackGeoJson;
        Source = source;
        DistanceMeters = distanceMeters;
        ElevationGainMeters = elevationGainMeters;
        OriginalGpx = originalGpx;
    }

    public void Update(
        int startDay,
        int endDay,
        string name,
        string note,
        string trackGeoJson,
        RouteSource source,
        double distanceMeters,
        double elevationGainMeters,
        string? originalGpx = null)
    {
        StartDay = startDay;
        EndDay = endDay;
        Name = name;
        Note = note;
        TrackGeoJson = trackGeoJson;
        Source = source;
        DistanceMeters = distanceMeters;
        ElevationGainMeters = elevationGainMeters;
        OriginalGpx = originalGpx;
    }
}
