namespace TripGeniusBackend.Application.DTOs.Trip;

public class UpdateTripRequest
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Stream? ImageStream { get; set; }
    public string? ImageFileName { get; set; }
    public DateTime StartingDate { get; set; }
    public DateTime EndingDate { get; set; }
    public string Status { get; set; }
    public List<String> Tags { get; set; }
    public int MaxParticipants { get; set; }
    public double Price { get; set; }
}