using TripGeniusBackend.Application.DTOs.Notifications;

namespace TripGeniusBackend.Application.Interfaces.Queries;

public interface INotificationQueryService
{
    public Task<List<NotificationResponse>> GetUserNotifications(int userId);
}