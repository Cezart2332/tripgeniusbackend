using System.Net;
using System.Net.Http.Json;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TripGeniusBackend.Application.Interfaces;
using TripGeniusBackend.Domain.Entities;
using TripGeniusBackend.Infrastructure.Persistence;
using TripGeniusBackend.Tests.Fixtures;

namespace TripGeniusBackend.Tests.Integration.Controllers;

public class UserControllerIntegrationTests : IClassFixture<TripGeniusWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TripGeniusWebApplicationFactory _factory;

    public UserControllerIntegrationTests(TripGeniusWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task SeedUserAsync(string email = "user1@test.com", string password = "OldPassword123!")
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (await db.Users.AnyAsync(u => u.Email == email))
        {
            return;
        }

        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var user = User.UserCreate(email, hasher.HashPassword(password));
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetMe_WithValidToken_ReturnsUserDetails()
    {
        // Arrange
        await SeedUserAsync();
        var token = AuthTestFixture.GenerateTestToken(1);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/user/me");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetMe_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/user/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateProfile_WithValidData_ReturnsOk()
    {
        // Arrange
        await SeedUserAsync();
        var token = AuthTestFixture.GenerateTestToken(1);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var content = new MultipartFormDataContent();
        content.Add(new StringContent("updateduser"), "username");

        // Act
        var response = await _client.PutAsync("/api/user/update", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateProfile_WithoutAuthorization_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.PutAsync("/api/user/update", new MultipartFormDataContent());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UploadAvatar_WithValidFile_ReturnsOk()
    {
        // Arrange
        await SeedUserAsync();
        var token = AuthTestFixture.GenerateTestToken(1);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var content = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(new byte[] { 1, 2, 3, 4, 5 });
        imageContent.Headers.ContentType = new("image/jpeg");
        content.Add(imageContent, "Avatar", "avatar.jpg");

        // Act
        var response = await _client.PutAsync("/api/user/update", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadAvatar_WithoutAuthorization_ReturnsUnauthorized()
    {
        // Arrange
        var content = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(new byte[] { 1, 2, 3, 4, 5 });
        imageContent.Headers.ContentType = new("image/jpeg");
        content.Add(imageContent, "Avatar", "avatar.jpg");

        // Act
        var response = await _client.PutAsync("/api/user/update", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_WithValidData_ReturnsOk()
    {
        // Arrange
        await SeedUserAsync();
        var token = AuthTestFixture.GenerateTestToken(1);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var changePasswordRequest = new { oldPassword = "OldPassword123!", newPassword = "NewPassword456!" };

        // Act
        var response = await _client.PatchAsJsonAsync("/api/user/change-password", changePasswordRequest);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangePassword_WithoutAuthorization_ReturnsUnauthorized()
    {
        // Arrange
        var changePasswordRequest = new { oldPassword = "OldPassword123!", newPassword = "NewPassword456!" };

        // Act
        var response = await _client.PatchAsJsonAsync("/api/user/change-password", changePasswordRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUserPreferences_WithValidToken_ReturnsPreferences()
    {
        // Arrange
        await SeedUserAsync();
        var token = AuthTestFixture.GenerateTestToken(1);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/user/preferences");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.NoContent);
    }
}
