using TripGeniusBackend.Application.Interfaces;
using TripGeniusBackend.Application.DTOs.Auth;
using TripGeniusBackend.Application.Interfaces.Queries;
using TripGeniusBackend.Domain.Entities;
namespace TripGeniusBackend.Application.UseCases;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserQueryService _userQueryService;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenHasher _tokenHasher;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IRefreshTokenQueryService _refreshTokenQueryService;

    public AuthService(IUserRepository userRepository,IUserQueryService userQueryService, IJwtService jwtService, IPasswordHasher passwordHasher, ITokenHasher tokenHasher, IRefreshTokenRepository refreshTokenRepository, IRefreshTokenQueryService refreshTokenQueryService)
    {
        _userRepository = userRepository;
        _userQueryService = userQueryService;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
        _tokenHasher = tokenHasher;
        _refreshTokenRepository = refreshTokenRepository;
        _refreshTokenQueryService = refreshTokenQueryService;
    }
    public async Task<AuthResponse> Register(RegisterRequest registerRequest)
    {
        if (await _userRepository.UserExists(registerRequest.Email))
            throw new ArgumentException("Email already exists");
        string hashedPassword = _passwordHasher.HashPassword(registerRequest.Password);
        User user = User.UserCreate(registerRequest.Email, hashedPassword);
        user.UpdateProfile(registerRequest.Username, "", "");
        user.UpdatePreferences(registerRequest.MaxGroupSize, registerRequest.Tags);


        await _userRepository.CreateUser(user);
        await _userRepository.SaveChanges();
        AuthResponse authResponse = await _jwtService.GenerateTokens(user);
        return authResponse;
    }
    

    public async Task<AuthResponse> Login(LoginRequest loginRequest)
    {

        var user = await _userRepository.GetUserByEmail(loginRequest.Email);
        if (!_passwordHasher.VerifyPassword(loginRequest.Password,user.Password)) throw new ArgumentException("Invalid password");
        AuthResponse response = await _jwtService.GenerateTokens(user);
        return response;

    }

    public async Task<AuthResponse> RefreshToken(string? refreshToken)
    {
        if (refreshToken == null) throw new ArgumentException("Refresh token is null");
        var hashedRefreshToken = _tokenHasher.HashToken(refreshToken);
        var refreshTokenEntity =  await _refreshTokenQueryService.GetRefreshToken(hashedRefreshToken);
        if (refreshTokenEntity == null) throw new ArgumentException("Refresh token not found");
        if (refreshTokenEntity.Expires < DateTime.UtcNow) throw new ArgumentException("Refresh token expired");
        
        await _refreshTokenRepository.DeleteRefreshToken(refreshTokenEntity);
        AuthResponse authResponse = await _jwtService.GenerateTokens(refreshTokenEntity.User);

        return authResponse;


    }

    public async Task Logout(string? refreshToken)
    {
        if (refreshToken != null)
        {
            var hashedRefreshToken = _tokenHasher.HashToken(refreshToken);
            var refreshTokenEntity = await _refreshTokenQueryService.GetRefreshToken(hashedRefreshToken);

            if (refreshTokenEntity != null) 
            {
                await _jwtService.RevokeRefreshToken(refreshTokenEntity);
            }
        }


    }
}