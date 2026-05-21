using TripGeniusBackend.Domain.Entities;

namespace TripGeniusBackend.Application.Interfaces.Repositories;

public interface IOffroadMessageRepository
{
    Task AddMessage(OffroadMessage message);
    Task<bool> DeleteMessageAsync(int messageId);
    Task SaveChanges();
}
