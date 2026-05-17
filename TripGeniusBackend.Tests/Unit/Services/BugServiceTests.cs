using Moq;
using Xunit;
using FluentAssertions;
using TripGeniusBackend.Application.DTOs.User;
using TripGeniusBackend.Application.Interfaces;
using TripGeniusBackend.Application.Interfaces.Queries;
using TripGeniusBackend.Application.Interfaces.Repositories;
using TripGeniusBackend.Application.UseCases;
using TripGeniusBackend.Domain.Entities;

namespace TripGeniusBackend.Tests.Unit.Services;

public class BugServiceTests
{
    private readonly Mock<IBugRepository> _mockBugRepository;
    private readonly Mock<IJwtService> _mockJwtService;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IUserQueryService> _mockUserQueryService;
    private readonly BugService _bugService;

    public BugServiceTests()
    {
        _mockBugRepository = new Mock<IBugRepository>();
        _mockJwtService = new Mock<IJwtService>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockUserQueryService = new Mock<IUserQueryService>();

        _bugService = new BugService(
            _mockBugRepository.Object,
            _mockJwtService.Object,
            _mockUserRepository.Object,
            _mockUserQueryService.Object
        );
    }

    [Fact]
    public async Task ReportBug_WithValidRequest_CreatesBugReport()
    {
        // Arrange
        const int userId = 1;
        var bugRequest = new BugRequest
        {
            Description = "The login button doesn't respond on mobile devices"
        };
        var user = User.UserCreate("user@example.com", "hashed_password");

        _mockJwtService.Setup(x => x.GetUserId()).Returns(userId);
        _mockUserRepository.Setup(x => x.GetUserById(userId)).ReturnsAsync(user);

        // Act
        await _bugService.ReportBug(bugRequest);

        // Assert
        _mockBugRepository.Verify(x => x.CreateBug(It.IsAny<Bug>()), Times.Once);
    }

    [Fact]
    public async Task ReportBug_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        _mockJwtService.Setup(x => x.GetUserId()).Returns(1);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _bugService.ReportBug(null!));
    }

    [Fact]
    public async Task ReportBug_WithEmptyDescription_ThrowsArgumentException()
    {
        // Arrange
        const int userId = 1;
        var bugRequest = new BugRequest
        {
            Description = ""
        };

        _mockJwtService.Setup(x => x.GetUserId()).Returns(userId);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _bugService.ReportBug(bugRequest));
    }
}
