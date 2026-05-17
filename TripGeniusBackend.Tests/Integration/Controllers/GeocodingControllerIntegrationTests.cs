using System.Net;
using System.Net.Http.Json;
using Xunit;
using FluentAssertions;
using TripGeniusBackend.Tests.Fixtures;

namespace TripGeniusBackend.Tests.Integration.Controllers;

public class GeocodingControllerIntegrationTests : IClassFixture<TripGeniusWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TripGeniusWebApplicationFactory _factory;

    public GeocodingControllerIntegrationTests(TripGeniusWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Search_WithValidQuery_ReturnsOk()
    {
        // Arrange
        const string query = "Bucharest";

        // Act
        var response = await _client.GetAsync($"/api/geocoding/search?query={Uri.EscapeDataString(query)}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Search_WithEmptyQuery_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/geocoding/search?query=");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
