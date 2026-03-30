using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WebApplication1.Data;
using WebApplication1.DTOs.User;
using WebApplication1.Models;

namespace WebApplication1.Services;

public class AuthService : IAuthService
{
    private readonly IConfiguration _config;
    private readonly AppDbContext _context;
    
    
    
    public AuthService(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }
    
    
    public async Task<AuthResponse> Register(RegisterRequest registerRequest)
    {
        if (_context.Users.Any(u => u.Email == registerRequest.Email)) throw new Exception("Email already exists");
        User user = new User
        {
            Name = registerRequest.Name,
            Email = registerRequest.Email,
            Password = BCrypt.Net.BCrypt.EnhancedHashPassword(registerRequest.Password, 14)
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
        return await GenerateTokens(user);
    }

    public async Task<AuthResponse> Login(LoginRequest loginRequest)
    {
        var user = _context.Users.FirstOrDefault(u => u.Email == loginRequest.Email);
        if (user == null) throw new Exception("User not found");
        if (!BCrypt.Net.BCrypt.EnhancedVerify(loginRequest.Password, user.Password)) throw new Exception("Invalid password");
        return await GenerateTokens(user);
    }

    public async Task<AuthResponse> RefreshToken(RefreshRequest refreshRequest)
    {
        var refreshToken = refreshRequest.RefreshToken;
        if(refreshToken == null) throw new Exception("Refresh token is null");
        var refreshTokenEntity = _context.RefreshTokens.Include(r => r.User).FirstOrDefault(r => r.Token == refreshToken);
        if (refreshTokenEntity == null) throw new Exception("Refresh token not found");
        if (refreshTokenEntity.Expires < DateTime.UtcNow) throw new Exception("Refresh token expired"); 
        
        _context.RefreshTokens.Remove(refreshTokenEntity);
        await _context.SaveChangesAsync();
        return await GenerateTokens(refreshTokenEntity.User);
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
            new Claim(ClaimTypes.Name, user.Name)
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
        var accesToken = GenerateAccessToken(user);
        RefreshToken refreshToken = new RefreshToken
        {
            Token = GenerateRefreshToken(),
            User = user,
            Expires = DateTime.UtcNow.AddDays(7)
        };
        await _context.RefreshTokens.AddAsync(refreshToken);
        await _context.SaveChangesAsync();

        return new AuthResponse
        {
            Token = accesToken,
            RefreshToken = refreshToken.Token
        };
    }
    
}