namespace TripGeniusBackend.Application.DTOs.Trip;

public class MessageResponse
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string ProfileUrl { get; set; } = string.Empty;
    /// <summary>True when the message was authored by the TripGenius AI agent.</summary>
    public bool IsAi { get; set; }
}
