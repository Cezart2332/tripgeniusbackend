using TripGeniusBackend.Application.Interfaces.Queries;
using TripGeniusBackend.Domain.Entities;

namespace TripGeniusBackend.Infrastructure.Persistence.Queries;

public class BugQueryService : IBugQueryService
{
    private readonly AppDbContext _context;

    public BugQueryService(AppDbContext context)
    {
        _context = context;
    }
}