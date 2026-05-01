using TripGeniusBackend.Domain.Entities;

namespace TripGeniusBackend.Application.Interfaces.Repositories;

public interface IMessageRepository
{
    public Task AddMessage(Message message);
    public Task SaveChanges();
}