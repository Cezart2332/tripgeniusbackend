namespace TripGeniusBackend.Application.Settings;

public class OpenRouterSettings
{
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Chat advisor model (must support OpenRouter server tools).</summary>
    public string ChatModel { get; set; } = "google/gemini-2.5-flash";

    /// <summary>max_results for openrouter:web_search server tool.</summary>
    public int ChatWebMaxResults { get; set; } = 5;
}