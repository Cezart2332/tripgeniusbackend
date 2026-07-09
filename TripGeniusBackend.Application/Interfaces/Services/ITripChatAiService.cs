namespace TripGeniusBackend.Application.Interfaces.Services;

/// <summary>
/// Handles an "@ai" mention inside a trip's group chat: runs the trip agent scoped to that
/// single trip, streams the reply to the trip group, and persists it as an AI-authored message.
/// </summary>
public interface ITripChatAiService
{
    Task RespondInTripAsync(int tripId, int userId, string userMessage, CancellationToken ct = default);
    Task RespondInOffroadAsync(int offroadTripId, int userId, string userMessage, CancellationToken ct = default);
}
