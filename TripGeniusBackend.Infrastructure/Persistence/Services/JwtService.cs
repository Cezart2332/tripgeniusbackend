using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TripGeniusBackend.Application.DTOs.Auth;
using TripGeniusBackend.Application.Interfaces;
using TripGeniusBackend.Domain.Entities;


namespace TripGeniusBackend.Infrastructure.Persistence.Services;

public class JwtService : IJwtService
{
    private readonly IConfiguration _config;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITokenHasher _tokenHasher;
    
    public JwtService(IConfiguration config, IHttpContextAccessor httpContextAccessor, IRefreshTokenRepository refreshTokenRepository, ITokenHasher tokenHasher)
    {
        _config = config;
        _httpContextAccessor = httpContextAccessor;
        _refreshTokenRepository = refreshTokenRepository;
        _tokenHasher = tokenHasher;
    }
    private string GenerateAccessToken(User user)
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

    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
    

    private void SetCookie(string token)
    {
        _httpContextAccessor.HttpContext.Response.Cookies.Append("refreshToken", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(7)
        });
    }
    
    public async Task<AuthResponse> GenerateTokens(User user)
    {
        var accessToken = GenerateAccessToken(user);
        string token = GenerateRefreshToken();
        RefreshToken refreshToken = new RefreshToken
        {
            Token = _tokenHasher.HashToken(token),
            User = user,
            Expires = DateTime.UtcNow.AddDays(7)
        };
        SetCookie(token);
        await _refreshTokenRepository.AddRefreshToken(refreshToken);
        
        return new AuthResponse
        {
            Token = accessToken,
        };
    }

    public async Task RevokeRefreshToken(RefreshToken refreshToken)
    {
        await _refreshTokenRepository.DeleteRefreshToken(refreshToken);
        _httpContextAccessor.HttpContext!.Response.Cookies.Delete("refreshToken", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
        });
    }

    public int GetUserId()
    {
        return int.Parse(_httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier));
    }
}