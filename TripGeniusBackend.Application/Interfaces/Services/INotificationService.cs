namespace TripGeniusBackend.Application.Interfaces;

public interface INotificationService
{
    public Task SendNotificationAsync(int userId, string title, string body, string url = "/app");
}