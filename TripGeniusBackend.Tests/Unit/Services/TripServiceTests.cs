using Moq;
using Xunit;
using FluentAssertions;
using TripGeniusBackend.Application.DTOs.Trip;
using TripGeniusBackend.Application.Interfaces;
using TripGeniusBackend.Application.Interfaces.Queries;
using TripGeniusBackend.Application.Interfaces.Repositories;
using TripGeniusBackend.Application.Interfaces.Services;
using TripGeniusBackend.Application.UseCases;
using TripGeniusBackend.Tests.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace TripGeniusBackend.Tests.Unit.Services;

public class TripServiceTests
{
    private readonly Mock<ITripRepository> _mockTripRepository;
    private readonly Mock<ITripQueryService> _mockTripQueryService;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IJwtService> _mockJwtService;
    private readonly Mock<IFileUploader> _mockFileUploader;
    private readonly Mock<IMessageQueryService> _mockMessageQueryService;
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<IPdfService> _mockPdfService;
    private readonly Mock<IBackgroundModerationService> _mockBackgroundModeration;
    private readonly TripService _tripService;

    public TripServiceTests()
    {
        _mockTripRepository = new Mock<ITripRepository>();
        _mockTripQueryService = new Mock<ITripQueryService>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockJwtService = new Mock<IJwtService>();
        _mockFileUploader = new Mock<IFileUploader>();
        _mockMessageQueryService = new Mock<IMessageQueryService>();
        _mockScopeFactory = new Mock<IServiceScopeFactory>();
        _mockNotificationService = new Mock<INotificationService>();
        _mockPdfService = new Mock<IPdfService>();
        _mockBackgroundModeration = new Mock<IBackgroundModerationService>();

        _tripService = new TripService(
            _mockTripRepository.Object,
            _mockTripQueryService.Object,
            _mockUserRepository.Object,
            _mockJwtService.Object,
            _mockFileUploader.Object,
            _mockMessageQueryService.Object,
            _mockScopeFactory.Object,
            _mockNotificationService.Object,
            _mockPdfService.Object,
            _mockBackgroundModeration.Object
        );
    }

    [Fact]
    public async Task CreateTrip_WithValidRequest_CreatesSuccessfully()
    {
        // Arrange
        const int userId = 1;
        var tripRequest = new TripRequest
        {
            Title = "Summer Adventure",
            Description = "An amazing summer trip",
            StartingDate = DateTime.UtcNow.AddDays(7),
            EndingDate = DateTime.UtcNow.AddDays(14),
            Tags = new List<string> { "adventure", "summer" },
            MaxParticipants = 10,
            Price = 500,
            Timelines = new List<TripTimelineRequest>()
        };

        _mockJwtService.Setup(x => x.GetUserId()).Returns(userId);

        // Act
        await _tripService.CreateTrip(tripRequest);

        // Assert
        _mockTripRepository.Verify(x => x.CreateTrip(It.IsAny<Domain.Entities.Trip>()), Times.Once);
        _mockTripRepository.Verify(x => x.SaveChanges(), Times.Exactly(2));
    }

    [Fact]
    public async Task CreateTrip_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        _mockJwtService.Setup(x => x.GetUserId()).Returns(1);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _tripService.CreateTrip(null!));
    }

    [Fact]
    public async Task CreateTrip_WithInvalidDates_ThrowsArgumentException()
    {
        // Arrange
        const int userId = 1;
        var tripRequest = new TripRequest
        {
            Title = "Invalid Trip",
            Description = "Dates are invalid",
            StartingDate = DateTime.UtcNow.AddDays(14),
            EndingDate = DateTime.UtcNow.AddDays(7), // End date before start date
            Tags = new List<string>(),
            MaxParticipants = 10,
            Price = 500,
            Timelines = new List<TripTimelineRequest>()
        };

        _mockJwtService.Setup(x => x.GetUserId()).Returns(userId);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _tripService.CreateTrip(tripRequest));
    }

    [Fact]
    public async Task GetTrip_WithValidId_ReturnsTrip()
    {
        // Arrange
        const int tripId = 1;
        const int userId = 2;
        var tripResponse = new TripResponse
        {
            Id = tripId,
            Title = "Test Trip",
            Description = "A test trip",
            StartingDate = DateTime.UtcNow.AddDays(7),
            EndingDate = DateTime.UtcNow.AddDays(14)
        };

        _mockJwtService.Setup(x => x.GetUserId()).Returns(userId);
        _mockTripQueryService.Setup(x => x.GetTrip(tripId, userId))
            .ReturnsAsync(tripResponse);

        // Act
        var result = await _tripService.GetTrip(tripId);

        // Assert
        result.Should().NotBeNull();
        result?.Id.Should().Be(tripId);
        result?.Title.Should().Be("Test Trip");
    }

    [Fact]
    public async Task UpdateTrip_WithValidRequest_UpdatesSuccessfully()
    {
        // Arrange
        const int tripId = 1;
        const int userId = 1;
        var updateRequest = new UpdateTripRequest
        {
            Id = tripId,
            Title = "Updated Trip",
            Description = "Updated description",
            Status = "Upcoming",
            Price = 600,
            MaxParticipants = 15
        };

        var trip = new TripBuilder().WithId(tripId).WithUserId(userId).Build();

        _mockJwtService.Setup(x => x.GetUserId()).Returns(userId);
        _mockTripRepository.Setup(x => x.GetTripById(tripId))
            .ReturnsAsync(trip);

        // Act
        await _tripService.UpdateTrip(updateRequest);

        // Assert
        _mockTripRepository.Verify(x => x.SaveChanges(), Times.Once);
    }
}
