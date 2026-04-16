using TripGeniusBackend.Domain.Entities;

namespace TripGeniusBackend.Application.Interfaces;

public interface IRefreshTokenRepository
{
    public Task AddRefreshToken(RefreshToken refreshToken);
    public Task<RefreshToken?> GetRefreshToken(string hashedRefreshToken);
    public Task DeleteRefreshToken(RefreshToken refreshToken);
}