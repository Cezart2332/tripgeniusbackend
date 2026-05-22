using TripGeniusBackend.Application.Interfaces.Services;
using TripGeniusBackend.Application.Moderation;

namespace TripGeniusBackend.Tests.Fixtures;

internal sealed class NoOpBackgroundModerationService : IBackgroundModerationService
{
    public void ScheduleImageReview(
        ModerationTarget target,
        int userId,
        int entityId,
        byte[] imageBytes,
        string? contentType)
    {
    }

    public void ScheduleTextReview(
        ModerationTarget target,
        int userId,
        int entityId,
        IReadOnlyList<(string Field, string Value)> fields,
        int? relatedEntityId = null)
    {
    }
}
