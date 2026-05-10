namespace TripGeniusBackend.Application.DTOs.AiChatResponse;

public class AiTripPlanner
{
    public string Description { get; set; }
    public int DurationDays { get; set; }
    public List<string> Interests { get; set; }
    public int Budget { get; set; }
    public string StartingPoint { get; set; }
    public int MaxParticipants { get; set; }
    
}