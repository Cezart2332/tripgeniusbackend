using TripGeniusBackend.Domain.Entities;

namespace TripGeniusBackend.Application.Interfaces.Repositories;

public interface IOffroadMessageRepository
{
    Task AddMessage(OffroadMessage message);
    Task SaveChanges();
}
