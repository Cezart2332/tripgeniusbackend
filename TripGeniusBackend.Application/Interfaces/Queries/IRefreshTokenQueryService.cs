using TripGeniusBackend.Domain.Entities;

namespace TripGeniusBackend.Application.Interfaces.Queries;

public interface IRefreshTokenQueryService
{
    public Task<RefreshToken?> GetRefreshToken(string hashedRefreshToken);
}