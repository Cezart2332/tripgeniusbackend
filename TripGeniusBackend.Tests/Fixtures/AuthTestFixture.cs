using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace TripGeniusBackend.Tests.Fixtures;

/// <summary>
/// Fixture for generating test JWT tokens for integration tests
/// </summary>
public class AuthTestFixture
{
    private const string SecretKey = "super-secret-key-development-2026";
    private const string Issuer = "tripgenius";
    private const string Audience = "tripgenius";

    /// <summary>
    /// Generates a valid JWT token for testing authorized endpoints
    /// </summary>
    public static string GenerateTestToken(int userId = 1, string? email = null, int expirationMinutes = 60)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email ?? $"user{userId}@test.com"),
            new Claim("UserId", userId.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Generates an expired JWT token for testing token expiration scenarios
    /// </summary>
    public static string GenerateExpiredToken(int userId = 1)
    {
        return GenerateTestToken(userId, expirationMinutes: -10);
    }
}
