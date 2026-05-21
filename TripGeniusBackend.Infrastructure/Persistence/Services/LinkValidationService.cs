using System.Text.RegularExpressions;
using TripGeniusBackend.Application.Interfaces.Services;

namespace TripGeniusBackend.Infrastructure.Persistence.Services;

/// <summary>
/// Lightweight link sanitization only. URL verification is done by the model via openrouter:web_fetch.
/// </summary>
public class LinkValidationService : ILinkValidationService
{
    private static readonly Regex[] BlockedUrlPatterns =
    [
        new(@"booking\.com/city/", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"booking\.com.*searchresults", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"momondo\.com/hotels/[^/]+-vacation-rentals", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"/vacation-rentals", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"tripadvisor\.com/Hotels-g\d+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"tripadvisor\.com/Restaurants-g\d+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    ];

    public Task<(bool isValid, string? finalUrl)> ValidateAsync(string url, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out _))
            return Task.FromResult<(bool, string?)>((false, null));

        if (IsBlockedListingUrl(url))
            return Task.FromResult<(bool, string?)>((false, null));

        return Task.FromResult<(bool, string?)>((true, url));
    }

    public Task<List<LinkCard>> ValidateAndRepairLinksAsync(List<LinkCard> links, CancellationToken ct = default)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var results = new List<LinkCard>();

        foreach (var link in links)
        {
            if (string.IsNullOrWhiteSpace(link.Title) || string.IsNullOrWhiteSpace(link.Url))
                continue;

            if (!Uri.TryCreate(link.Url, UriKind.Absolute, out _))
                continue;

            if (IsBlockedListingUrl(link.Url))
                continue;

            var normalized = link.Url.TrimEnd('/');
            if (!seen.Add(normalized))
                continue;

            results.Add(new LinkCard { Title = link.Title.Trim(), Url = link.Url.Trim() });
        }

        return Task.FromResult(results);
    }

    private static bool IsBlockedListingUrl(string url) =>
        BlockedUrlPatterns.Any(p => p.IsMatch(url));
}
