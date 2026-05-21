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

    public async Task<bool> DeleteMessageAsync(int messageId)
    {
        var message = await _context.OffroadMessages.FindAsync(messageId);
        if (message is null)
            return false;

        _context.OffroadMessages.Remove(message);
        return true;
    }

    public async Task SaveChanges() => await _context.SaveChangesAsync();
}
