using TripGeniusBackend.Application.DTOs.Trip;

namespace TripGeniusBackend.API.DTOs;

public class InitialTripRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IFormFile? Image { get; set; }
    public DateTime StartingDate { get; set; }
    public DateTime EndingDate { get; set; }
    public string Status { get; set; }
    public List<String> Tags { get; set; }
    public int MaxParticipants { get; set; }
    public double Price { get; set; }
    public List<TripTimelineRequest> Timelines { get; set; }
}