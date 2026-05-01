using TripGeniusBackend.Domain.Entities;

namespace TripGeniusBackend.Application.Interfaces;

public interface IRefreshTokenRepository
{
    public Task DeleteAllRefreshTokens(int userId);
    public Task AddRefreshToken(RefreshToken refreshToken);

    public Task DeleteRefreshToken(RefreshToken refreshToken);
    public Task SaveChanges();
}