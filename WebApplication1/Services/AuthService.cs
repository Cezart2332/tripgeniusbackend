using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WebApplication1.Data;
using WebApplication1.DTOs.Auth;
using WebApplication1.Models;

namespace WebApplication1.Services;

public class AuthService : IAuthService
{
    private readonly IConfiguration _config;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AppDbContext _context;
    
    
    
    
    public AuthService(AppDbContext context, IConfiguration config, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _config = config;
        _httpContextAccessor = httpContextAccessor;
    }
    
    
    public async Task<AuthResponse> Register(RegisterRequest registerRequest)
    {
        if (await _context.Users.AnyAsync(u => u.Email == registerRequest.Email)) throw new Exception("Email already exists");
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            
            Profile profile = new Profile
            {
                Username = registerRequest.Username,
                ProfileURL = "",
                Description = ""
            };
            Preferences preferences = new Preferences
            {
                MaxGroupSize = registerRequest.MaxGroupSize,
                Tags = registerRequest.Tags
            };
            User user = new User
            {
                Profile = profile,
                Preferences = preferences,
                Email = registerRequest.Email,
                Password = BCrypt.Net.BCrypt.EnhancedHashPassword(registerRequest.Password, 14)
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            AuthResponse authResponse = await GenerateTokens(user);
            await transaction.CommitAsync();
            return authResponse;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<AuthResponse> Login(LoginRequest loginRequest)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginRequest.Email);
        if (user == null) throw new Exception("User not found");
        if (!BCrypt.Net.BCrypt.EnhancedVerify(loginRequest.Password, user.Password)) throw new Exception("Invalid password");
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            AuthResponse response = await GenerateTokens(user);
            await transaction.CommitAsync();
            return response;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<AuthResponse> RefreshToken(string refreshToken)
    {
        if(refreshToken == null) throw new Exception("Refresh token is null");
        var hashedRefreshToken = HashToken(refreshToken);
        var refreshTokenEntity = await _context.RefreshTokens.Include(r => r.User).FirstOrDefaultAsync(r => r.Token == hashedRefreshToken);
        if (refreshTokenEntity == null) throw new Exception("Refresh token not found");
        if (refreshTokenEntity.Expires < DateTime.UtcNow) throw new Exception("Refresh token expired");

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            
            _context.RefreshTokens.Remove(refreshTokenEntity);
            AuthResponse authResponse = await GenerateTokens(refreshTokenEntity.User);
        
            await transaction.CommitAsync();
            return authResponse;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
        
    }
    
    public string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(15);
        
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
        };


        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private async Task<AuthResponse> GenerateTokens(User user)
    {
        var accessToken = GenerateAccessToken(user);
        string token = GenerateRefreshToken();
        RefreshToken refreshToken = new RefreshToken
        {
            Token = HashToken(token),
            User = user,
            Expires = DateTime.UtcNow.AddDays(7)
        };
        SetCookie(token);
        await _context.RefreshTokens.AddAsync(refreshToken);
        await _context.SaveChangesAsync();

        return new AuthResponse
        {
            Token = accessToken,
        };
    }

    private string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }

    private void SetCookie(string token)
    {
        _httpContextAccessor.HttpContext.Response.Cookies.Append("refreshToken", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTime.UtcNow.AddDays(7)
        });
    }
}