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
        var tokens = await _context.RefreshTokens.Where(t => t.UserId == userId).ToListAsync();
        _context.RefreshTokens.RemoveRange(tokens);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteExpiredRefreshTokens(int userId)
    {
        var expired = await _context.RefreshTokens
            .Where(t => t.UserId == userId && t.Expires < DateTime.UtcNow)
            .ToListAsync();
        if (expired.Count == 0) return;
        _context.RefreshTokens.RemoveRange(expired);
        await _context.SaveChangesAsync();
    }
    
    public async Task AddRefreshToken(RefreshToken refreshToken)
    {
        await _context.RefreshTokens.AddAsync(refreshToken);
    }

    public async Task DeleteRefreshToken(RefreshToken refreshToken)
    {
        _context.RefreshTokens.Remove(refreshToken);
        await _context.SaveChangesAsync();
    }

    public async Task SaveChanges()
    {
        await _context.SaveChangesAsync();
    }
    


}