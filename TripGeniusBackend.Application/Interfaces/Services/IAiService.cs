using TripGeniusBackend.Application.DTOs.AiChatResponse;
using TripGeniusBackend.Application.DTOs.Trip;

namespace TripGeniusBackend.Application.Interfaces;

public interface IAiService
{
    public Task AskAsync(List<AiChatResponse> lastMessages,string prompt,string memoryContext,string relevantTrips,string userPreferences, Func<string, Task> onChunk);
    public Task GenerateTripAsync(AiTripPlanner aiTripPlanner);
    public Task<string> ExtractAsync(string prompt);

}