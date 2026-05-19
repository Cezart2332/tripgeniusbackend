using TripGeniusBackend.Application.Interfaces.Repositories;
using TripGeniusBackend.Domain.Entities;

namespace TripGeniusBackend.Infrastructure.Persistence.Repositories;

public class OffroadMessageRepository : IOffroadMessageRepository
{
    private readonly AppDbContext _context;

    public OffroadMessageRepository(AppDbContext context) => _context = context;

    public Task AddMessage(OffroadMessage message)
    {
        _context.OffroadMessages.Add(message);
        return Task.CompletedTask;
    }

    public async Task SaveChanges() => await _context.SaveChangesAsync();
}
