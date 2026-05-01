using TripGeniusBackend.Domain.Entities;

namespace TripGeniusBackend.Application.Interfaces;

public interface INotificationRepository
{
    public Task AddNotification(Notification notification);
    public Task SaveChanges();
    
}