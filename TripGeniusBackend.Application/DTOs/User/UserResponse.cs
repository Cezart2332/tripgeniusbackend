using TripGeniusBackend.Application.DTOs.Notifications;
using TripGeniusBackend.Application.DTOs.Trip;

namespace TripGeniusBackend.Application.DTOs.User;
public class UserResponse
{
    public int Id { get; set; }
    public string Username { get; set; }
    public bool IsVerified { get; set; }
    public string ProfileUrl { get; set; }
    public string Description { get; set; }
    public int GroupSize { get; set; }
    public List<string> Tags { get; set; }
    public string Email { get; set; }
    public List<NotificationResponse> Notifications { get; set; }
    public List<TripResponse> Trips { get; set; }
}