namespace TripGeniusBackend.Application.DTOs.Geocoding;

public class ElevationLookupRequest
{
    public List<ElevationPointDto> Points { get; set; } = [];
}

public class ElevationPointDto
{
    public double Lng { get; set; }
    public double Lat { get; set; }
}

public class ElevationLookupResponse
{
    public List<double> Elevations { get; set; } = [];
}
