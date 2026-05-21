using System.Text.RegularExpressions;

namespace TripGeniusBackend.Application.Helpers;

/// <summary>
/// Fast local check for obvious slurs. Used when the moderation HTTP service is down (fail-open)
/// and for chat background removal without relying on ML alone.
/// </summary>
public static partial class ProfanityFilter
{
    public static bool ContainsProfanity(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        return ProfanityPattern().IsMatch(text);
    }

    [GeneratedRegex(
        @"\b(fuck|fucking|motherfucker|shit|bullshit|bitch|asshole|cunt|dick|pussy|cock|whore|slut|nigger|nigga|faggot|retard|pula|muie|fut|curve|pizda)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ProfanityPattern();
}
