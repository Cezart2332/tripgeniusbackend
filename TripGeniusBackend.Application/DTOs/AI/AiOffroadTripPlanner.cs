namespace TripGeniusBackend.Application.DTOs.AiChatResponse;

public class AiOffroadTripPlanner
{
    public string Description { get; set; } = string.Empty;
    public int DurationDays { get; set; }
    public List<string> Interests { get; set; } = new();
    public int Budget { get; set; }
    public string Region { get; set; } = string.Empty;
    public int MaxParticipants { get; set; }
    public string DifficultyLevel { get; set; } = "Moderate";
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
