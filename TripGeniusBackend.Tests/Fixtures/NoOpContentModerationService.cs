using TripGeniusBackend.Application.Interfaces.Services;

namespace TripGeniusBackend.Tests.Fixtures;

/// <summary>
/// Skips HTTP calls to the moderation container during integration tests.
/// </summary>
internal sealed class NoOpContentModerationService : IContentModerationService
{
    public Task<ModerationCheckResult> CheckTextAsync(string text, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ModerationCheckResult(false));

    public Task<ModerationCheckResult> CheckImageAsync(
        Stream imageStream,
        string? contentType,
        CancellationToken cancellationToken = default)
    {
        if (imageStream.CanSeek)
            imageStream.Position = 0;

        return Task.FromResult(new ModerationCheckResult(false));
    }
}
