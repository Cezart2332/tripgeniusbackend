namespace TripGeniusBackend.Application.DTOs.OffroadTrip;

public class OffroadTripRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Stream? ImageStream { get; set; }
    public string? ImageFileName { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime StartingDate { get; set; }
    public DateTime EndingDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public int MaxParticipants { get; set; }
    public double Price { get; set; }
    public List<OffroadRouteRequest> Routes { get; set; } = new();
}
