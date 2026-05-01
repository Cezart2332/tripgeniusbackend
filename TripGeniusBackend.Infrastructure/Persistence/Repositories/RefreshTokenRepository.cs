using Microsoft.EntityFrameworkCore;
using TripGeniusBackend.Application.Interfaces;
using TripGeniusBackend.Domain.Entities;

namespace TripGeniusBackend.Infrastructure.Persistence.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _context;
    
    public RefreshTokenRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task DeleteAllRefreshTokens(int userId)
    {
        await _context.RefreshTokens.Where(t => t.UserId == userId).ExecuteDeleteAsync();
    }
    
    public async Task AddRefreshToken(RefreshToken refreshToken)
    {
        await _context.RefreshTokens.AddAsync(refreshToken);
    }

    public async Task DeleteRefreshToken(RefreshToken refreshToken)
    {

        await _context.RefreshTokens
            .Where(t => t.Id == refreshToken.Id)
            .ExecuteDeleteAsync();
    }

    public async Task SaveChanges()
    {
        await _context.SaveChangesAsync();
    }
    


}