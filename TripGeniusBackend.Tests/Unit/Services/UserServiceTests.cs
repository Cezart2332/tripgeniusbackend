using Moq;
using Xunit;
using FluentAssertions;
using TripGeniusBackend.Application.DTOs.User;
using TripGeniusBackend.Application.Interfaces;
using TripGeniusBackend.Application.Interfaces.Queries;
using TripGeniusBackend.Application.Interfaces.Repositories;
using TripGeniusBackend.Application.Interfaces.Services;
using TripGeniusBackend.Application.UseCases;
using TripGeniusBackend.Domain.Entities;

namespace TripGeniusBackend.Tests.Unit.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IUserQueryService> _mockUserQueryService;
    private readonly Mock<IJwtService> _mockJwtService;
    private readonly Mock<IFileUploader> _mockFileUploader;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockUserQueryService = new Mock<IUserQueryService>();
        _mockJwtService = new Mock<IJwtService>();
        _mockFileUploader = new Mock<IFileUploader>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();

        _userService = new UserService(
            _mockUserRepository.Object,
            _mockUserQueryService.Object,
            _mockJwtService.Object,
            _mockFileUploader.Object,
            _mockPasswordHasher.Object,
            new Mock<IBackgroundModerationService>().Object
        );
    }

    [Fact]
    public async Task GetMe_WithValidUserId_ReturnsUserDetails()
    {
        // Arrange
        const int userId = 1;
        var expectedUserResponse = new UserResponse
        {
            Id = userId,
            Email = "user@example.com",
            Username = "testuser"
        };

        _mockJwtService.Setup(x => x.GetUserId()).Returns(userId);
        _mockUserQueryService.Setup(x => x.GetUserDetails(userId))
            .ReturnsAsync(expectedUserResponse);

        // Act
        var result = await _userService.GetMe();

        // Assert
        result.Should().NotBeNull();
        result?.Id.Should().Be(userId);
        result?.Email.Should().Be("user@example.com");
        _mockUserQueryService.Verify(x => x.GetUserDetails(userId), Times.Once);
    }

    [Fact]
    public async Task GetMe_WithInvalidUserId_ReturnsNull()
    {
        // Arrange
        const int userId = 999;
        
        _mockJwtService.Setup(x => x.GetUserId()).Returns(userId);
        _mockUserQueryService.Setup(x => x.GetUserDetails(userId))
            .ReturnsAsync((UserResponse?)null);

        // Act
        var result = await _userService.GetMe();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Update_WithValidData_UpdatesSuccessfully()
    {
        // Arrange
        const int userId = 1;
        var updateRequest = new UpdateRequest
        {
            Username = "updateduser",
            Description = "updated description",
            GroupSize = 5,
            Tags = new List<string> { "adventure" }
        };

        var user = User.UserCreate("user@example.com", "hashed_password");

        _mockJwtService.Setup(x => x.GetUserId()).Returns(userId);
        _mockUserRepository.Setup(x => x.GetUserById(userId))
            .ReturnsAsync(user);

        // Act
        var result = await _userService.Update(updateRequest);

        // Assert
        result.Should().NotBeNull();
        result.Username.Should().Be("updateduser");
        result.Description.Should().Be("updated description");
        _mockUserRepository.Verify(x => x.SaveChanges(), Times.Once);
    }

    [Fact]
    public async Task Update_WithNullUser_ThrowsKeyNotFoundException()
    {
        // Arrange
        const int userId = 1;
        var updateRequest = new UpdateRequest();

        _mockJwtService.Setup(x => x.GetUserId()).Returns(userId);
        _mockUserRepository.Setup(x => x.GetUserById(userId))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _userService.Update(updateRequest));
    }

    [Fact]
    public async Task Update_WithAvatarFile_UploadsSuccessfully()
    {
        // Arrange
        const int userId = 1;
        const string fileUrl = "https://example.com/avatar.jpg";
        const string fileName = "avatar.jpg";

        var mockStream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        var updateRequest = new UpdateRequest
        {
            AvatarStream = mockStream,
            AvatarFileName = fileName
        };
        
        var user = User.UserCreate("user@example.com", "hashed_password");

        _mockJwtService.Setup(x => x.GetUserId()).Returns(userId);
        _mockUserRepository.Setup(x => x.GetUserById(userId))
            .ReturnsAsync(user);
        _mockFileUploader.Setup(x => x.UploadFile(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(fileUrl);

        // Act
        var result = await _userService.Update(updateRequest);

        // Assert
        result.ProfileUrl.Should().Be(fileUrl);
        _mockFileUploader.Verify(x => x.UploadFile(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task ChangePassword_WithCorrectOldPassword_ChangesSuccessfully()
    {
        // Arrange
        const int userId = 1;
        const string oldPassword = "OldPassword123!";
        const string newPassword = "NewPassword456!";
        const string hashedOldPassword = "hashed_old_password";
        const string hashedNewPassword = "hashed_new_password";

        var request = new ChangePasswordRequest
        {
            OldPassword = oldPassword,
            NewPassword = newPassword
        };

        var user = User.UserCreate("user@example.com", hashedOldPassword);

        _mockJwtService.Setup(x => x.GetUserId()).Returns(userId);
        _mockUserRepository.Setup(x => x.GetUserById(userId))
            .ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.VerifyPassword(oldPassword, hashedOldPassword))
            .Returns(true);
        _mockPasswordHasher.Setup(x => x.HashPassword(newPassword))
            .Returns(hashedNewPassword);

        // Act
        await _userService.ChangePassword(request);

        // Assert
        _mockUserRepository.Verify(x => x.SaveChanges(), Times.Once);
    }

    [Fact]
    public async Task ChangePassword_WithIncorrectOldPassword_ThrowsException()
    {
        // Arrange
        const int userId = 1;
        const string oldPassword = "WrongPassword!";
        const string newPassword = "NewPassword456!";
        const string hashedOldPassword = "hashed_old_password";

        var request = new ChangePasswordRequest
        {
            OldPassword = oldPassword,
            NewPassword = newPassword
        };

        var user = User.UserCreate("user@example.com", hashedOldPassword);

        _mockJwtService.Setup(x => x.GetUserId()).Returns(userId);
        _mockUserRepository.Setup(x => x.GetUserById(userId))
            .ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.VerifyPassword(oldPassword, hashedOldPassword))
            .Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _userService.ChangePassword(request));
        _mockUserRepository.Verify(x => x.SaveChanges(), Times.Never);
    }
}
