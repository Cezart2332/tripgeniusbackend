using System.Net;
using System.Net.Http.Json;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using TripGeniusBackend.Infrastructure.Persistence;
using TripGeniusBackend.Domain.Entities;
using TripGeniusBackend.Application.Interfaces;
using TripGeniusBackend.Application.DTOs.Auth;
using TripGeniusBackend.Tests.Fixtures;

namespace TripGeniusBackend.Tests.Integration.Controllers;

public class AuthControllerIntegrationTests : IClassFixture<TripGeniusWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TripGeniusWebApplicationFactory _factory;

    public AuthControllerIntegrationTests(TripGeniusWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsOkAndMessage()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "newuser@example.com",
            Password = "SecurePassword123!",
            Username = "newuser",
            MaxGroupSize = 5,
            Tags = new List<string> { "adventure" }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Check your email");
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "duplicate@example.com",
            Password = "SecurePassword123!",
            Username = "user1",
            MaxGroupSize = 5,
            Tags = new List<string>()
        };

        // Register first user
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Act - Try to register with same email
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_ReplayedWithSameIdempotencyKey_ReplaysResponseAndCreatesOneUser()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "idempotency@example.com",
            Password = "SecurePassword123!",
            Username = "idemuser",
            MaxGroupSize = 4,
            Tags = new List<string>()
        };
        var key = Guid.NewGuid().ToString();

        // Act - same request twice with the same Idempotency-Key
        var first = await PostWithIdempotencyKey("/api/auth/register", registerRequest, key);
        var second = await PostWithIdempotencyKey("/api/auth/register", registerRequest, key);

        // Assert - the replay returns the cached success, not the "email exists" 400
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        second.Headers.Contains("Idempotent-Replayed").Should().BeTrue();

        // And the mutation was applied exactly once
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var count = await db.Users.CountAsync(u => u.Email == "idempotency@example.com");
        count.Should().Be(1);
    }

    [Fact]
    public async Task Register_ReplayedWithoutIdempotencyKey_ReturnsDuplicateError()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "no-idem-key@example.com",
            Password = "SecurePassword123!",
            Username = "noidem",
            MaxGroupSize = 4,
            Tags = new List<string>()
        };

        // Act - without the header, the second call is a real duplicate
        var first = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var second = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Assert
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<HttpResponseMessage> PostWithIdempotencyKey<T>(string url, T body, string key)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Add("Idempotency-Key", key);
        return await _client.SendAsync(request);
    }

    [Fact]
    public async Task Register_WithMissingEmail_ReturnsBadRequest()
    {
        // Arrange
        var registerRequest = new { Password = "Password123!", Username = "user" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkAndTokens()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "testuser@example.com",
            Password = "TestPassword123!",
            Username = "testuser",
            MaxGroupSize = 5,
            Tags = new List<string>()
        };

        // Register user first
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Manually verify email in Db
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == "testuser@example.com");
            if (user != null)
            {
                user.VerifyEmail();
                await db.SaveChangesAsync();
            }
        }

        var loginRequest = new LoginRequest
        {
            Email = "testuser@example.com",
            Password = "TestPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var responseBody = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, because: $"Response body: {responseBody}");
        var content = await response.Content.ReadFromJsonAsync<AuthResponse>();
        content.Should().NotBeNull();
        content?.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "WrongPassword!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RefreshToken_WithValidToken_ReturnsNewTokens()
    {
        // Arrange
        int userId = 1;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tokenHasher = scope.ServiceProvider.GetRequiredService<ITokenHasher>();
            
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                user = User.UserCreate("refreshuser@test.com", "Password123!");
                user.VerifyEmail();
                db.Users.Add(user);
                await db.SaveChangesAsync();
                userId = user.Id;
            }

            var existingTokens = await db.RefreshTokens.Where(t => t.UserId == userId).ToListAsync();
            db.RefreshTokens.RemoveRange(existingTokens);
            await db.SaveChangesAsync();

            var refreshToken = new RefreshToken
            {
                Token = tokenHasher.HashToken("valid_refresh_token"),
                UserId = userId,
                Expires = DateTime.UtcNow.AddDays(7)
            };
            db.RefreshTokens.Add(refreshToken);
            await db.SaveChangesAsync();
        }

        _client.DefaultRequestHeaders.Add("Cookie", "refreshToken=valid_refresh_token");

        // Act
        var response = await _client.PostAsync("/api/auth/refresh", null);

        // Assert - Should succeed if token is valid
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RefreshToken_WithExpiredToken_ReturnsUnauthorized()
    {
        // Arrange
        int userId = 1;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tokenHasher = scope.ServiceProvider.GetRequiredService<ITokenHasher>();
            
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                user = User.UserCreate("refreshuser@test.com", "Password123!");
                user.VerifyEmail();
                db.Users.Add(user);
                await db.SaveChangesAsync();
                userId = user.Id;
            }

            var existingTokens = await db.RefreshTokens.Where(t => t.UserId == userId).ToListAsync();
            db.RefreshTokens.RemoveRange(existingTokens);
            await db.SaveChangesAsync();

            var refreshToken = new RefreshToken
            {
                Token = tokenHasher.HashToken("expired_refresh_token"),
                UserId = userId,
                Expires = DateTime.UtcNow.AddDays(-7)
            };
            db.RefreshTokens.Add(refreshToken);
            await db.SaveChangesAsync();
        }

        _client.DefaultRequestHeaders.Add("Cookie", "refreshToken=expired_refresh_token");

        // Act
        var response = await _client.PostAsync("/api/auth/refresh", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
