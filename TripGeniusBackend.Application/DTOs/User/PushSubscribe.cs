namespace TripGeniusBackend.Application.DTOs.User;

public class PushSubscribe
{
    public string Endpoint { get; set; } = string.Empty;
    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
}