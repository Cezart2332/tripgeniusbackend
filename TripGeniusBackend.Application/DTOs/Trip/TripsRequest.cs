namespace TripGeniusBackend.Application.DTOs.Trip;

public class TripsRequest
{
    public bool Preferences { get; set; }
    public string Tag { get;set; }
    public string Search {get;set;}
    public double Budget {get;set;}
}