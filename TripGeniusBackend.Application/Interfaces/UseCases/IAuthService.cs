namespace TripGeniusBackend.Application.Interfaces;
using DTOs.Auth;

public interface IAuthService
{
    public Task<AuthResponse> Register(RegisterRequest registerRequest);
    public Task<AuthResponse> Login(LoginRequest loginRequest);
    public Task<AuthResponse> RefreshToken(string? refreshToken);
    public Task Logout(string? refreshToken);
}