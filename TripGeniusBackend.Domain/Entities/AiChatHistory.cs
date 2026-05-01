namespace TripGeniusBackend.Domain.Entities;

public class AiChatHistory
{
    public int Id { get; set; }
    public User User { get; set; }
    public int UserId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
}