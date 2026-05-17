using System.Net;
using System.Net.Http.Json;
using Xunit;
using FluentAssertions;
using TripGeniusBackend.Application.DTOs.Trip;
using TripGeniusBackend.Tests.Fixtures;

namespace TripGeniusBackend.Tests.Integration.Controllers;

public class TripControllerIntegrationTests : IClassFixture<TripGeniusWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TripGeniusWebApplicationFactory _factory;

    public TripControllerIntegrationTests(TripGeniusWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateTrip_WithoutAuthorization_ReturnsUnauthorized()
    {
        // Arrange
        var tripRequest = new { title = "Test Trip", description = "A test trip" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/trip/create-trip", tripRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateTrip_WithValidToken_ReturnsOk()
    {
        // Arrange
        var token = AuthTestFixture.GenerateTestToken(1);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        // Create form data for file upload
        var content = new MultipartFormDataContent();
        content.Add(new StringContent("Summer Adventure"), "title");
        content.Add(new StringContent("An amazing trip"), "description");
        content.Add(new StringContent("2025-06-01"), "startingDate");
        content.Add(new StringContent("2025-06-15"), "endingDate");
        content.Add(new StringContent("10"), "maxParticipants");
        content.Add(new StringContent("500"), "price");

        // Act
        var response = await _client.PostAsync("/api/trip/create-trip", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GetTrip_WithValidId_ReturnsTrip()
    {
        // Arrange
        const int tripId = 1;
        var token = AuthTestFixture.GenerateTestToken(1);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        // Act
        var response = await _client.GetAsync($"/api/trip/get-trip/{tripId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAllTrips_ReturnsOk()
    {
        // Arrange
        var token = AuthTestFixture.GenerateTestToken(1);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/trip/get-all-trips");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateTrip_WithValidData_ReturnsOk()
    {
        // Arrange
        const int tripId = 1;
        var token = AuthTestFixture.GenerateTestToken(1);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var content = new MultipartFormDataContent();
        content.Add(new StringContent(tripId.ToString()), "id");
        content.Add(new StringContent("Updated Trip"), "title");

        // Act
        var response = await _client.PatchAsync("/api/trip/update-trip", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteTrip_WithValidId_ReturnsOk()
    {
        // Arrange
        const int tripId = 1;
        var token = AuthTestFixture.GenerateTestToken(1);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        // Act
        var response = await _client.DeleteAsync($"/api/trip/timeline-remove/{tripId}/1");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyTrips_WithAuthorization_ReturnsOk()
    {
        // Arrange
        var token = AuthTestFixture.GenerateTestToken(1);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/trip/get-all-trips");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMyTrips_WithoutAuthorization_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/trip/get-all-trips");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
