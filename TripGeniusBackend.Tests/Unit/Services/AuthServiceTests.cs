using Moq;
using Xunit;
using FluentAssertions;
using TripGeniusBackend.Application.DTOs.Auth;
using TripGeniusBackend.Application.Exceptions;
using TripGeniusBackend.Application.Interfaces;
using TripGeniusBackend.Application.Interfaces.Queries;
using TripGeniusBackend.Application.Interfaces.Repositories;
using TripGeniusBackend.Application.Settings;
using TripGeniusBackend.Application.UseCases;
using TripGeniusBackend.Domain.Entities;
using Microsoft.Extensions.Options;

namespace TripGeniusBackend.Tests.Unit.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IJwtService> _mockJwtService;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly Mock<ITokenHasher> _mockTokenHasher;
    private readonly Mock<IRefreshTokenQueryService> _mockRefreshTokenQueryService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly IOptions<GoogleSettings> _googleSettings;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockJwtService = new Mock<IJwtService>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _mockTokenHasher = new Mock<ITokenHasher>();
        _mockRefreshTokenQueryService = new Mock<IRefreshTokenQueryService>();
        _mockEmailService = new Mock<IEmailService>();

        var googleSettings = new GoogleSettings { ClientId = "test-client-id" };
        _googleSettings = Options.Create(googleSettings);

        _authService = new AuthService(
            _mockUserRepository.Object,
            _mockJwtService.Object,
            _mockPasswordHasher.Object,
            _mockTokenHasher.Object,
            _mockRefreshTokenQueryService.Object,
            _mockEmailService.Object,
            _googleSettings
        );
    }

    [Fact]
    public async Task Register_WithValidRequest_CreatesUserSuccessfully()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "newuser@example.com",
            Password = "SecurePassword123!",
            Username = "newuser",
            MaxGroupSize = 5,
            Tags = new List<string> { "adventure", "nature" }
        };

        _mockUserRepository.Setup(x => x.UserExists(registerRequest.Email))
            .ReturnsAsync(false);
        _mockPasswordHasher.Setup(x => x.HashPassword(registerRequest.Password))
            .Returns("hashed_password");
        _mockEmailService.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _authService.Register(registerRequest);

        // Assert
        result.Should().Contain("Check your email to verify");
        _mockUserRepository.Verify(x => x.CreateUser(It.IsAny<User>()), Times.Once);
        _mockUserRepository.Verify(x => x.SaveChanges(), Times.Once);
        _mockEmailService.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Register_WithExistingEmail_ThrowsAppException()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "existing@example.com",
            Password = "SecurePassword123!",
            Username = "newuser",
            MaxGroupSize = 5,
            Tags = new List<string>()
        };

        _mockUserRepository.Setup(x => x.UserExists(registerRequest.Email))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<AppException>(() => _authService.Register(registerRequest));
        _mockUserRepository.Verify(x => x.CreateUser(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsAuthResponse()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "user@example.com",
            Password = "Password123!"
        };

        var user = User.UserCreate(loginRequest.Email, "hashed_password");
        user.VerifyEmail(); // Mark as verified

        var expectedAuthResponse = new AuthResponse
        {
            Token = "test_access_token"
        };

        _mockUserRepository.Setup(x => x.GetUserByEmail(loginRequest.Email))
            .ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.VerifyPassword(loginRequest.Password, user.Password))
            .Returns(true);
        _mockJwtService.Setup(x => x.GenerateTokens(user))
            .ReturnsAsync(expectedAuthResponse);

        // Act
        var result = await _authService.Login(loginRequest);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().Be("test_access_token");
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ThrowsArgumentException()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "user@example.com",
            Password = "WrongPassword!"
        };

        var user = User.UserCreate(loginRequest.Email, "hashed_password");
        user.VerifyEmail();

        _mockUserRepository.Setup(x => x.GetUserByEmail(loginRequest.Email))
            .ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.VerifyPassword(loginRequest.Password, user.Password))
            .Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _authService.Login(loginRequest));
    }

    [Fact]
    public async Task Login_WithUnverifiedEmail_ThrowsAppException()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "user@example.com",
            Password = "Password123!"
        };

        var user = User.UserCreate(loginRequest.Email, "hashed_password");
        // User is not verified

        _mockUserRepository.Setup(x => x.GetUserByEmail(loginRequest.Email))
            .ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.VerifyPassword(loginRequest.Password, user.Password))
            .Returns(true);
        _mockEmailService.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AppException>(() => _authService.Login(loginRequest));
        exception.Message.Should().Contain("Email not verified");
    }

    [Fact]
    public async Task Login_WithNullUser_ThrowsArgumentException()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "Password123!"
        };

        _mockUserRepository.Setup(x => x.GetUserByEmail(loginRequest.Email))
            .ReturnsAsync((User?)null);
        _mockPasswordHasher.Setup(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _authService.Login(loginRequest));
    }
}
