using System.Text.RegularExpressions;

namespace TripGeniusBackend.Infrastructure.Persistence.Services;

/// <summary>
/// Removes internal OpenRouter / link-repair prompt text that must never appear in user-facing chat.
/// </summary>
internal static class AiResponseSanitizer
{
    private static readonly Regex[] LeakPatterns =
    [
        new(
            @"Find the official or direct property page for\s*.+?\.\s*Return ONLY the JSON URL\.?",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline),
        new(
            @"You are a URL finder\..*?Must not be a search/listing/404 page\.?",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline),
    ];

    public static string StripInternalPrompts(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var result = text;
        foreach (var pattern in LeakPatterns)
            result = pattern.Replace(result, string.Empty);

        return result;
    }
}
