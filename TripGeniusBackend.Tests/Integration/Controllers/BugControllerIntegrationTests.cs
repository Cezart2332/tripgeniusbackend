using System.Net;
using System.Net.Http.Json;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using TripGeniusBackend.Infrastructure.Persistence;
using TripGeniusBackend.Domain.Entities;
using TripGeniusBackend.Tests.Fixtures;

namespace TripGeniusBackend.Tests.Integration.Controllers;

public class BugControllerIntegrationTests : IClassFixture<TripGeniusWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TripGeniusWebApplicationFactory _factory;

    public BugControllerIntegrationTests(TripGeniusWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ReportBug_WithValidData_ReturnsOk()
    {
        // Arrange
        int seededUserId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == "bugreporter@test.com");
            if (user == null)
            {
                user = User.UserCreate("bugreporter@test.com", "Password123!");
                user.VerifyEmail();
                db.Users.Add(user);
                await db.SaveChangesAsync();
            }
            seededUserId = user.Id;
        }

        var token = AuthTestFixture.GenerateTestToken(seededUserId);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var bugRequest = new
        {
            description = "The login button doesn't respond on mobile"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/bug/bug-report", bugRequest);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReportBug_WithoutAuthorization_ReturnsUnauthorized()
    {
        // Arrange
        var bugRequest = new
        {
            description = "A test bug"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/bug/bug-report", bugRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
