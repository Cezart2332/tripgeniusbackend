namespace TripGeniusBackend.Application.DTOs.Notifications;

public class NotificationResponse
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
}