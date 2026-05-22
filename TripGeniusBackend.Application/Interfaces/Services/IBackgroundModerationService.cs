using TripGeniusBackend.Application.Moderation;

namespace TripGeniusBackend.Application.Interfaces.Services;

/// <summary>
/// Runs content moderation asynchronously after the user-facing request completes.
/// Flagged content is removed and the user is notified in-app and via push.
/// </summary>
public interface IBackgroundModerationService
{
    void ScheduleImageReview(
        ModerationTarget target,
        int userId,
        int entityId,
        byte[] imageBytes,
        string? contentType);

    /// <param name="entityId">Primary id (trip id, user id, or route id for standalone route review).</param>
    /// <param name="relatedEntityId">Timeline id when target is TripTimeline; trip id when target is OffroadRoute.</param>
    void ScheduleTextReview(
        ModerationTarget target,
        int userId,
        int entityId,
        IReadOnlyList<(string Field, string Value)> fields,
        int? relatedEntityId = null);
}
