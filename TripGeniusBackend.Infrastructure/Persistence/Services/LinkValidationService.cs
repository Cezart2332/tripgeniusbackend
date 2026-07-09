using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using TripGeniusBackend.Application.Interfaces.Services;

namespace TripGeniusBackend.Infrastructure.Persistence.Services;

/// <summary>
/// Validates recommended links with a real HTTP request (the model's own web_fetch is unreliable):
/// drops URLs that respond 404 / point to a non-existent host, or that resolve to a search /
/// city-listing page. Anti-bot 403s and transient errors are kept (benefit of the doubt) so real
/// venues behind bot protection are not thrown away.
/// </summary>
public class LinkValidationService : ILinkValidationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LinkValidationService> _logger;

    private const int ProbeTimeoutSeconds = 6;

    private static readonly Regex[] BlockedUrlPatterns =
    [
        new(@"booking\.com/city/", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"booking\.com.*searchresults", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"momondo\.[^/]+/hotels/[^/]+-vacation-rentals", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"/vacation-rentals", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"tripadvisor\.[^/]+/Hotels-g\d+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"tripadvisor\.[^/]+/Restaurants-g\d+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"airbnb\.[^/]+/s/", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"expedia\.[^/]+/.*-Hotel-Search", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"[?&](searchresults|ss=|search_query=)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    ];

    public LinkValidationService(HttpClient httpClient, ILogger<LinkValidationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<(bool isValid, string? finalUrl)> ValidateAsync(string url, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return (false, null);

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return (false, null);

        // Google Maps search links are the intended fallback — they always resolve, skip the probe.
        if (IsGoogleMapsLink(url))
            return (true, url);

        if (IsBlockedListingUrl(url))
            return (false, null);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(ProbeTimeoutSeconds));

        try
        {
            using var response = await SendProbeAsync(uri, timeoutCts.Token);

            // The handler follows redirects, so RequestUri is where we actually landed.
            var finalUrl = response.RequestMessage?.RequestUri?.ToString() ?? url;

            // A dead property page often 301s to a search / city page — treat that as invalid.
            if (IsBlockedListingUrl(finalUrl))
                return (false, null);

            // Definitively gone → drop it.
            if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
                return (false, null);

            // Everything else (2xx/3xx, plus 401/403/405/429 anti-bot and 5xx transient) → keep.
            // Return the original URL, not the redirect target, to avoid tracking-param noise.
            return (true, url);
        }
        catch (HttpRequestException ex) when (IsHostNotFound(ex))
        {
            _logger.LogInformation("Link dropped (host not found): {Url}", url);
            return (false, null);
        }
        catch (OperationCanceledException)
        {
            // Slow but possibly real site — keep it rather than drop a good venue.
            return (true, url);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Link probe failed, keeping by default: {Url}", url);
            return (true, url);
        }
    }

    public async Task<List<LinkCard>> ValidateAndRepairLinksAsync(
        List<LinkCard> links,
        Func<LinkCard, Task<string?>>? reSearch = null,
        CancellationToken ct = default)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var candidates = new List<LinkCard>();

        foreach (var link in links)
        {
            if (string.IsNullOrWhiteSpace(link.Title) || string.IsNullOrWhiteSpace(link.Url))
                continue;
            if (!Uri.TryCreate(link.Url, UriKind.Absolute, out _))
                continue;
            if (!seen.Add(link.Url.TrimEnd('/')))
                continue;

            candidates.Add(link);
        }

        // Validate + repair every candidate concurrently — none of this should add serial latency.
        var processed = await Task.WhenAll(candidates.Select(link => RepairLinkAsync(link, reSearch, ct)));

        var results = new List<LinkCard>();
        var seenResultUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var card in processed)
        {
            if (seenResultUrls.Add(card.Url.TrimEnd('/')))
                results.Add(card);
        }

        return results;
    }

    private async Task<LinkCard> RepairLinkAsync(
        LinkCard link,
        Func<LinkCard, Task<string?>>? reSearch,
        CancellationToken ct)
    {
        var (isValid, finalUrl) = await ValidateAsync(link.Url, ct);
        if (isValid)
            return new LinkCard { Title = link.Title.Trim(), Url = (finalUrl ?? link.Url).Trim() };

        // Repair step 1 — re-search: ask the model for a fresh, specific link, then re-validate it.
        if (reSearch != null)
        {
            string? candidate = null;
            try { candidate = await reSearch(link); }
            catch (Exception ex) { _logger.LogDebug(ex, "Link re-search callback threw for {Url}", link.Url); }

            if (!string.IsNullOrWhiteSpace(candidate))
            {
                var (ok, resolved) = await ValidateAsync(candidate!, ct);
                if (ok)
                {
                    _logger.LogInformation("Recommended link repaired via re-search: {Url}", candidate);
                    return new LinkCard { Title = link.Title.Trim(), Url = (resolved ?? candidate!).Trim() };
                }
            }
        }

        // Repair step 2 — deterministic map link to the exact place. A place is never dropped.
        _logger.LogInformation("Recommended link repaired with map fallback: {Url}", link.Url);
        return new LinkCard { Title = link.Title.Trim(), Url = BuildMapsFallback(link.Title) };
    }

    /// <summary>A Google Maps link that resolves to the named place — the guaranteed-working fallback.</summary>
    public static string BuildMapsFallback(string placeQuery) =>
        $"https://www.google.com/maps/search/?api=1&query={Uri.EscapeDataString(placeQuery.Trim())}";

    private async Task<HttpResponseMessage> SendProbeAsync(Uri uri, CancellationToken ct)
    {
        // A small ranged GET works on more servers than HEAD, while still downloading almost nothing.
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.TryAddWithoutValidation(
            "User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36");
        request.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml");
        request.Headers.Range = new RangeHeaderValue(0, 2047);

        return await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
    }

    private static bool IsBlockedListingUrl(string url) =>
        BlockedUrlPatterns.Any(p => p.IsMatch(url));

    private static bool IsGoogleMapsLink(string url) =>
        url.Contains("google.com/maps", StringComparison.OrdinalIgnoreCase) ||
        url.Contains("maps.google.", StringComparison.OrdinalIgnoreCase) ||
        url.Contains("goo.gl/maps", StringComparison.OrdinalIgnoreCase);

    /// <summary>True when the failure means the host itself does not exist (DNS / connection refused).</summary>
    private static bool IsHostNotFound(HttpRequestException ex)
    {
        if (ex.InnerException is SocketException se)
            return se.SocketErrorCode is SocketError.HostNotFound
                or SocketError.NoData
                or SocketError.ConnectionRefused;
        return false;
    }
}
