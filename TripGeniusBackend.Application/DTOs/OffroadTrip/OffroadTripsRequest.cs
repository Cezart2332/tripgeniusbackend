namespace TripGeniusBackend.Application.DTOs.OffroadTrip;

public class OffroadTripsRequest
{
    public bool Preferences { get; set; }
    public string Tag { get; set; } = "all";
    public string Search { get; set; } = string.Empty;
    public double Budget { get; set; }
}
