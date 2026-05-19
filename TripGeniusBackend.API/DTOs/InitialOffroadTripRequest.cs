using TripGeniusBackend.Application.DTOs.OffroadTrip;

namespace TripGeniusBackend.API.DTOs;

public class InitialOffroadTripRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IFormFile? Image { get; set; }
    public DateTime StartingDate { get; set; }
    public DateTime EndingDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public int MaxParticipants { get; set; }
    public double Price { get; set; }
    public List<OffroadRouteRequest> Routes { get; set; } = new();
}
