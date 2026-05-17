using Moq;
using TripGeniusBackend.Application.Interfaces;
using TripGeniusBackend.Application.Interfaces.Queries;
using TripGeniusBackend.Application.Interfaces.Repositories;
using TripGeniusBackend.Application.Interfaces.UseCases;

namespace TripGeniusBackend.Tests.Mocks;

/// <summary>
/// Provides pre-configured mocks for common service dependencies
/// </summary>
public class MockServiceFixture
{
    public Mock<IUserRepository> MockUserRepository { get; } = new();
    public Mock<ITripRepository> MockTripRepository { get; } = new();
    public Mock<IBugRepository> MockBugRepository { get; } = new();
    public Mock<IAiChatRepository> MockAiChatRepository { get; } = new();
    public Mock<IMessageRepository> MockMessageRepository { get; } = new();
    public Mock<IRefreshTokenRepository> MockRefreshTokenRepository { get; } = new();
    
    public Mock<IUserQueryService> MockUserQueryService { get; } = new();
    public Mock<ITripQueryService> MockTripQueryService { get; } = new();
    public Mock<IAiChatQueryService> MockAiChatQueryService { get; } = new();
    public Mock<IBugQueryService> MockBugQueryService { get; } = new();
    public Mock<IMessageQueryService> MockMessageQueryService { get; } = new();
    public Mock<IRefreshTokenQueryService> MockRefreshTokenQueryService { get; } = new();
    
    public Mock<IJwtService> MockJwtService { get; } = new();
    public Mock<IPasswordHasher> MockPasswordHasher { get; } = new();
    public Mock<ITokenHasher> MockTokenHasher { get; } = new();
    public Mock<IEmailService> MockEmailService { get; } = new();
    public Mock<IFileUploader> MockFileUploader { get; } = new();
    public Mock<IAiService> MockAiService { get; } = new();
    public Mock<INotificationService> MockNotificationService { get; } = new();
    public Mock<IPdfService> MockPdfService { get; } = new();

    /// <summary>
    /// Resets all mocks to their initial state
    /// </summary>
    public void ResetAllMocks()
    {
        MockUserRepository.Reset();
        MockTripRepository.Reset();
        MockBugRepository.Reset();
        MockAiChatRepository.Reset();
        MockMessageRepository.Reset();
        MockRefreshTokenRepository.Reset();
        
        MockUserQueryService.Reset();
        MockTripQueryService.Reset();
        MockAiChatQueryService.Reset();
        MockBugQueryService.Reset();
        MockMessageQueryService.Reset();
        MockRefreshTokenQueryService.Reset();
        
        MockJwtService.Reset();
        MockPasswordHasher.Reset();
        MockTokenHasher.Reset();
        MockEmailService.Reset();
        MockFileUploader.Reset();
        MockAiService.Reset();
        MockNotificationService.Reset();
        MockPdfService.Reset();
    }
}
