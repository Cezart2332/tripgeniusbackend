using Microsoft.EntityFrameworkCore;
using TripGeniusBackend.Application.Interfaces.Queries;
using TripGeniusBackend.Domain.Entities;

namespace TripGeniusBackend.Infrastructure.Persistence.Queries;

public class RefreshTokenQueryService : IRefreshTokenQueryService
{
    private readonly AppDbContext _context;

    public RefreshTokenQueryService(AppDbContext context)
    {
        _context = context;
    }
    public async Task<RefreshToken?> GetRefreshToken(string hashedRefreshToken)
    {
        return await _context.RefreshTokens.Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == hashedRefreshToken);
    }
}