namespace TripGeniusBackend.Application.Interfaces.Services;

public sealed record ModerationCheckResult(bool IsBlocked, string? Reason = null);

public interface IContentModerationService
{
    Task<ModerationCheckResult> CheckTextAsync(string text, CancellationToken cancellationToken = default);

    Task<ModerationCheckResult> CheckImageAsync(
        Stream imageStream,
        string? contentType,
        CancellationToken cancellationToken = default);
}
