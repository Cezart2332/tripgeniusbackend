using System.Net;
using System.Net.Http.Json;
using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.DependencyInjection;
using TripGeniusBackend.Tests.Fixtures;
using TripGeniusBackend.Application.Interfaces;
using TripGeniusBackend.Application.DTOs.AiChatResponse;

namespace TripGeniusBackend.Tests.Integration.Controllers;

public class AiControllerIntegrationTests : IClassFixture<TripGeniusWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TripGeniusWebApplicationFactory _factory;

    public AiControllerIntegrationTests(TripGeniusWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetChatHistory_WithValidToken_ReturnsOk()
    {
        // Arrange
        var token = AuthTestFixture.GenerateTestToken(1);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/ai/history");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetChatHistory_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/ai/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GenerateTrip_WithValidData_ReturnsOk()
    {
        // Arrange
        var token = AuthTestFixture.GenerateTestToken(1);

        var customClient = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the real IAiService registration
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAiService));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Register mocked IAiService
                var mockAiService = new Mock<IAiService>();
                mockAiService.Setup(s => s.GenerateTripAsync(It.IsAny<AiTripPlanner>()))
                    .ReturnsAsync(1);
                services.AddSingleton<IAiService>(mockAiService.Object);
            });
        }).CreateClient();

        customClient.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var plannerRequest = new AiTripPlanner
        {
            Description = "A nice 3 days trip to Rome",
            DurationDays = 3,
            Interests = new List<string> { "Culture", "Food" },
            Budget = 500,
            StartingPoint = "Rome",
            MaxParticipants = 4
        };

        // Act
        var response = await customClient.PostAsJsonAsync("/api/ai/generate-trip", plannerRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GenerateTrip_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var plannerRequest = new AiTripPlanner
        {
            Description = "A nice 3 days trip to Rome",
            DurationDays = 3,
            Interests = new List<string> { "Culture", "Food" },
            Budget = 500,
            StartingPoint = "Rome",
            MaxParticipants = 4
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/ai/generate-trip", plannerRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
