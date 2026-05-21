using TripGeniusBackend.Domain.Entities;

namespace TripGeniusBackend.Application.Interfaces.Repositories;

public interface IMessageRepository
{
    Task AddMessage(Message message);
    Task<bool> DeleteMessageAsync(int messageId);
    Task SaveChanges();
}