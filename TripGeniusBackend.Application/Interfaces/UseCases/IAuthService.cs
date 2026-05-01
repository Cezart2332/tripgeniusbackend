namespace TripGeniusBackend.Application.Interfaces.UseCases;
using DTOs.Auth;

public interface IAuthService
{
    public Task<string> Register(RegisterRequest registerRequest);
    public Task<AuthResponse> Login(LoginRequest loginRequest);
    public Task<AuthResponse> LoginWithGoogle(string token);
    public Task<AuthResponse> RefreshToken(string? refreshToken);
    public Task Logout(string? refreshToken);
    
    public Task<AuthResponse> VerifyEmail(string token);
    public Task SendResetPassword(string email);
    public Task<AuthResponse> ResetPassword(string token, string newPassword);
}