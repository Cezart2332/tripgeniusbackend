namespace TripGeniusBackend.API.DTOs;

public class LocationSuggestion
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string PlaceName { get; set; } = string.Empty;
    public double Lat { get; set; }
    public double Lng { get; set; }
}