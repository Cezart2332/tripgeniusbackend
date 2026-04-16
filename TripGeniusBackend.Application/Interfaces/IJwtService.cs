using TripGeniusBackend.Application.DTOs.Auth;
using TripGeniusBackend.Domain.Entities;

namespace TripGeniusBackend.Application.Interfaces;

public interface IJwtService
{
    public Task<AuthResponse> GenerateTokens(User user);
    public Task RevokeRefreshToken(RefreshToken token);
    public int GetUserId();
}