using TripGeniusBackend.Domain.Entities;

namespace TripGeniusBackend.Application.Interfaces;

public interface IRefreshTokenRepository
{
    public Task AddRefreshToken(RefreshToken refreshToken);

    public Task DeleteRefreshToken(RefreshToken refreshToken);
}