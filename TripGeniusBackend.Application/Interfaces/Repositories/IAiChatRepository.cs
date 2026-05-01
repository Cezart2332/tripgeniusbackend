using TripGeniusBackend.Domain.Entities;

namespace TripGeniusBackend.Application.Interfaces.Repositories;

public interface IAiChatRepository
{
    public Task Create(AiChatHistory aiChatHistory);
    public Task SaveChanges();
}