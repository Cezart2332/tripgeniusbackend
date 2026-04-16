using TripGeniusBackend.Application.Interfaces;
using TripGeniusBackend.Domain.Entities;

namespace TripGeniusBackend.Infrastructure.Persistence.Repositories;

public class BugRepository : IBugRepository
{
    private readonly AppDbContext _context;

    public BugRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task CreateBug(Bug bug)
    {
        await _context.Bugs.AddAsync(bug);
        await _context.SaveChangesAsync();
    }
}