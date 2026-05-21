using TripGeniusBackend.Application.DTOs.AiChatResponse;
using TripGeniusBackend.Application.DTOs.Trip;

namespace TripGeniusBackend.Application.Interfaces;

public static class AiActivityStatus
{
    public const string Analyzing = "analyzing";
    public const string Thinking = "thinking";
    public const string SearchingWeb = "searching_web";
    public const string Writing = "writing";
    public const string ValidatingLinks = "validating_links";
}

public interface IAiService
{
    public Task AskAsync(List<AiChatResponse> lastMessages, string prompt, string memoryContext, string relevantTrips, string userPreferences, Func<string, Task> onChunk, Func<string, Task> onStatus);
    public Task<int> GenerateTripAsync(AiTripPlanner aiTripPlanner);
    public Task<int> GenerateOffroadTripAsync(AiOffroadTripPlanner planner);
    public Task<string> ExtractAsync(string prompt);

}