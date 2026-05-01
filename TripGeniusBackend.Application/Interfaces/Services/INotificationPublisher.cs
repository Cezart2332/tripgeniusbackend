namespace TripGeniusBackend.Application.Interfaces;

public interface INotificationPublisher
{
    public Task SendAsync(int userId, object payload);
}