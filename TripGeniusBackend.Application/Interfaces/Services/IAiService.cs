using TripGeniusBackend.Application.DTOs.AiChatResponse;

namespace TripGeniusBackend.Application.Interfaces;

public interface IAiService
{
    public Task AskAsync(List<AiChatResponse> lastMessages,string prompt,string memoryContext,string relevantTrips,string userPreferences, Func<string, Task> onChunk);
    public Task<string> ExtractAsync(string prompt);

}