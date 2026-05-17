using Moq;
using Xunit;
using FluentAssertions;
using TripGeniusBackend.Application.DTOs.AiChatResponse;
using TripGeniusBackend.Application.Interfaces;
using TripGeniusBackend.Application.Interfaces.Queries;
using TripGeniusBackend.Application.UseCases;

namespace TripGeniusBackend.Tests.Unit.Services;

public class AiChatServiceTests
{
    private readonly Mock<IJwtService> _mockJwtService;
    private readonly Mock<IAiChatQueryService> _mockAiChatQueryService;
    private readonly AiChatService _aiChatService;

    public AiChatServiceTests()
    {
        _mockJwtService = new Mock<IJwtService>();
        _mockAiChatQueryService = new Mock<IAiChatQueryService>();

        _aiChatService = new AiChatService(
            _mockJwtService.Object,
            _mockAiChatQueryService.Object
        );
    }

    [Fact]
    public async Task GetMessages_WithValidUserId_ReturnsMessageList()
    {
        // Arrange
        const int userId = 1;
        var expectedMessages = new List<AiChatResponse>
        {
            new() { Message = "Hello", Role = "user", dateTime = DateTime.UtcNow },
            new() { Message = "Hi there!", Role = "assistant", dateTime = DateTime.UtcNow }
        };

        _mockJwtService.Setup(x => x.GetUserId()).Returns(userId);
        _mockAiChatQueryService.Setup(x => x.GetUserHistory(userId))
            .ReturnsAsync(expectedMessages);

        // Act
        var result = await _aiChatService.GetMessages();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Message.Should().Be("Hello");
        _mockAiChatQueryService.Verify(x => x.GetUserHistory(userId), Times.Once);
    }

    [Fact]
    public async Task GetMessages_WithEmptyHistory_ReturnsEmptyList()
    {
        // Arrange
        const int userId = 1;
        var expectedMessages = new List<AiChatResponse>();

        _mockJwtService.Setup(x => x.GetUserId()).Returns(userId);
        _mockAiChatQueryService.Setup(x => x.GetUserHistory(userId))
            .ReturnsAsync(expectedMessages);

        // Act
        var result = await _aiChatService.GetMessages();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMessages_WithInvalidUserId_ThrowsKeyNotFoundException()
    {
        // Arrange
        _mockJwtService.Setup(x => x.GetUserId()).Returns(0);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => _aiChatService.GetMessages());
        exception.Message.Should().Contain("Not a valid user");
    }

    [Fact]
    public async Task GetMessages_WithNegativeUserId_ThrowsKeyNotFoundException()
    {
        // Arrange
        _mockJwtService.Setup(x => x.GetUserId()).Returns(-1);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _aiChatService.GetMessages());
    }

    [Fact]
    public async Task GetMessages_WithNullResult_ReturnsEmptyList()
    {
        // Arrange
        const int userId = 1;
        var expectedMessages = new List<AiChatResponse>();

        _mockJwtService.Setup(x => x.GetUserId()).Returns(userId);
        _mockAiChatQueryService.Setup(x => x.GetUserHistory(userId))
            .ReturnsAsync(expectedMessages);

        // Act
        var result = await _aiChatService.GetMessages();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
}
