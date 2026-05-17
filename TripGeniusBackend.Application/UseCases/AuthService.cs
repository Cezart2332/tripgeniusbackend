using Google.Apis.Auth;
using Microsoft.Extensions.Options;
using TripGeniusBackend.Application.Interfaces;
using TripGeniusBackend.Application.DTOs.Auth;
using TripGeniusBackend.Application.Exceptions;
using TripGeniusBackend.Application.Interfaces.Queries;
using TripGeniusBackend.Application.Interfaces.UseCases;
using TripGeniusBackend.Application.Settings;
using TripGeniusBackend.Domain.Entities;
namespace TripGeniusBackend.Application.UseCases;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;

    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenHasher _tokenHasher;
    private readonly IRefreshTokenQueryService _refreshTokenQueryService;
    private readonly IEmailService _emailService;
    private readonly GoogleSettings _googleSettings;

    public AuthService(IUserRepository userRepository, IJwtService jwtService, IPasswordHasher passwordHasher, ITokenHasher tokenHasher,  IRefreshTokenQueryService refreshTokenQueryService, IEmailService emailService, IOptions<GoogleSettings> googleSettings)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
        _tokenHasher = tokenHasher;
        _refreshTokenQueryService = refreshTokenQueryService;
        _emailService = emailService;
        _googleSettings = googleSettings.Value;
    }
    public async Task<string> Register(RegisterRequest registerRequest)
    {
        if (await _userRepository.UserExists(registerRequest.Email))
            throw new AppException(400, "Email already exists");
        string hashedPassword = _passwordHasher.HashPassword(registerRequest.Password);
        User user = User.UserCreate(registerRequest.Email, hashedPassword);
        user.UpdateProfile(registerRequest.Username, "", "");
        user.UpdatePreferences(registerRequest.MaxGroupSize, registerRequest.Tags);
        await _emailService.SendEmailAsync(user.Email, "Account Verification",
            $"We are happy to welcome you to TripGenius! Please click the link below to verify your account",
            $"tripgenius.online/verify-email?token={user.VerifyToken}", "Verify Email");

        await _userRepository.CreateUser(user);
        await _userRepository.SaveChanges();
        
        return "Check your email to verify your account to access the app!";
    }
    

    public async Task<AuthResponse> Login(LoginRequest loginRequest)
    {
        var user = await _userRepository.GetUserByEmail(loginRequest.Email);
        if (user == null) throw new ArgumentException("Invalid email or password");
        if (!_passwordHasher.VerifyPassword(loginRequest.Password, user.Password)) throw new ArgumentException("Invalid email or password");
        if (user.IsVerified == false)
        {
            user.RegenerateVerifyToken();
            try
            {
               await _emailService.SendEmailAsync(user.Email, "Account Verification",
                    $"We are happy to welcome you to TripGenius! Please click the link below to verify your account",
                    $"tripgenius.online/verify-email?token={user.VerifyToken}", "Verify Email");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
            await _userRepository.SaveChanges();
            throw new AppException(402,"Email not verified,we send you another link on email!");
        }
        AuthResponse response = await _jwtService.GenerateTokens(user);
        return response;
    }

    public async Task<AuthResponse> LoginWithGoogle(string token)
    {


        var settings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = new[] { _googleSettings.ClientId }
        };

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(token, settings);
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw new AppException(402, "Token Google invalid");
        }
        
        string email = payload.Email;
        
        string googleId = payload.Subject;
        string name = payload.Name;
        var user = await _userRepository.GetUserByEmail(email);
        if (user == null)
        {
            user = User.UserCreateGoogle(email,googleId);
            user.UpdateProfile(name,"","");
            user.UpdatePreferences(0, []);
            await _userRepository.CreateUser(user);
            await _userRepository.SaveChanges();
        }

        if(user.GoogleId == null || user.GoogleId.Trim().Equals(""))
        {
            if(user.IsVerified == false) user.VerifyEmail();
            user.UpdateGoogleId(googleId);
            await _userRepository.SaveChanges();
        }
        
        AuthResponse response = await _jwtService.GenerateTokens(user);
        
        return response;
    }

    public async Task<AuthResponse> RefreshToken(string? refreshToken)
    {
        if (refreshToken == null) throw new AppException(401, "Refresh token is null");
        var hashedRefreshToken = _tokenHasher.HashToken(refreshToken);
        var refreshTokenEntity =  await _refreshTokenQueryService.GetRefreshToken(hashedRefreshToken);
        if (refreshTokenEntity == null) throw new AppException(401, "Refresh token not found");
        if (refreshTokenEntity.Expires < DateTime.UtcNow)  throw new AppException(401, "Refresh token expired");
        
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

    public async Task<AuthResponse> VerifyEmail(string token)
    {
        var user = await _userRepository.GetUserByToken(token);
        if (user == null) throw new ArgumentException("Invalid Token");
        if (user.VerifyTokenExpires < DateTime.UtcNow)
        {
            user.RegenerateVerifyToken();
            await  _emailService.SendEmailAsync(user.Email, "Account Verification",
                $"We are happy to welcome you to TripGenius! Please click the link below to verify your account",
                $"tripgenius.online/verify-email?token={user.VerifyToken}", "Verify Email");
            await _userRepository.SaveChanges();
            throw new AppException(402,"Email link expired,we send you another link on email!");
        }
        user.VerifyEmail();
        await _userRepository.SaveChanges();
        AuthResponse response = await _jwtService.GenerateTokens(user);
        return response;
    }

    public async Task SendResetPassword(string email)
    {
        var user = await _userRepository.GetUserByEmail(email);
        if(user == null) throw new ArgumentException("Invalid email");
        user.RegenerateResetToken();
        await _emailService.SendEmailAsync(user.Email, "Reset Password",
            $"We received a request to reset your password. Please click the link below to reset your password",
            $"tripgenius.online/reset-password?token={user.ResetToken}", "Reset Password");
        await _userRepository.SaveChanges();
    }
    public async Task<AuthResponse> ResetPassword(string token, string newPassword)
    {
        var user = await _userRepository.GetUserByResetToken(token);
        if(user == null) throw new ArgumentException("Invalid token");
        if(user.ResetTokenExpires < DateTime.UtcNow) throw new AppException(402,"Reset link expired,we send you another link on email!");
        user.UpdatePassword(_passwordHasher.HashPassword(newPassword));
        user.ResetPassword();
        await _userRepository.SaveChanges();
        AuthResponse response = await _jwtService.GenerateTokens(user);
        return response;
    }
    

}