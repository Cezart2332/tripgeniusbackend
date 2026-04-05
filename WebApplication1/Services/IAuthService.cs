using WebApplication1.DTOs.Auth;

namespace WebApplication1.Services;

public interface IAuthService
{
    public Task<AuthResponse> Register(RegisterRequest registerRequest);
    public Task<AuthResponse> Login(LoginRequest loginRequest);
    public Task<AuthResponse> RefreshToken(string refreshToken);
}