using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TripGeniusBackend.Application.Interfaces.Services;
using TripGeniusBackend.Application.Settings;

namespace TripGeniusBackend.Infrastructure.Persistence.Services;

public class LinkValidationService : ILinkValidationService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<LinkValidationService> _logger;

    // URL patterns that indicate search/listing pages (should be rejected)
    private static readonly Regex[] SearchPagePatterns =
    {
        new(@"/searchresults", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"/search\?", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"[?&]q=", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"/s\?", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"/search/?$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"/listings?", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"/results?", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"booking\.com.*searchresults", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"airbnb\.com.*search", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"tripadvisor\.com.*Search", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"expedia\.com.*search", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    };

    // Body phrases that indicate error/broken pages (case-insensitive substring match)
    private static readonly string[] ErrorPhrases =
    {
        "critical error on this website",
        "there has been a critical error",
        "page not found",
        "page could not be found",
        "this page isn't available",
        "this page doesn't exist",
        "the page you requested",
        "let's get you back on track",
        "404 not found",
        "error 404",
        "did you mean",
        "showing results for",
        "no results found",
        "no results were found",
        "there has been an error",
        "an error occurred",
        "something went wrong",
        "fatal error",
        "wordpress error",
        "site can't be reached",
        "this site can\u2019t be reached",
        "access denied",
        "you don't have permission",
        "<title>error</title>",
        "<title>404</title>",
        "<title>page not found</title>",
    };

    private const int MaxReplacementSearches = 4;

    public LinkValidationService(HttpClient httpClient, IOptions<OpenRouterSettings> openRouterSettings, ILogger<LinkValidationService> logger)
    {
        _httpClient = httpClient;
        _apiKey = openRouterSettings.Value.ApiKey;
        _logger = logger;
    }

    public async Task<(bool isValid, string? finalUrl)> ValidateAsync(string url, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            return (false, null);

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return (false, null);

        // Reject known search page patterns immediately without HTTP request
        if (IsSearchPageUrl(url))
            return (false, null);

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(15));

            // Always GET with body inspection — many sites (booking.com etc.) return 200 for soft-404 pages
            var getRequest = new HttpRequestMessage(HttpMethod.Get, uri);
            getRequest.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36");
            getRequest.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            getRequest.Headers.Add("Accept-Language", "en-US,en;q=0.5");

            var getResponse = await _httpClient.SendAsync(getRequest, HttpCompletionOption.ResponseHeadersRead, cts.Token);

            var finalGetUrl = getResponse.RequestMessage?.RequestUri?.ToString() ?? url;

            // Check for error status codes
            if ((int)getResponse.StatusCode >= 400 && (int)getResponse.StatusCode < 600)
                return (false, null);

            // Check if redirect led to a search page
            if (IsSearchPageUrl(finalGetUrl))
                return (false, null);

            // Read first ~16KB of body to check for soft-404 / error phrases
            if (getResponse.IsSuccessStatusCode)
            {
                using var bodyStream = await getResponse.Content.ReadAsStreamAsync(cts.Token);
                var buffer = new byte[16384];
                int bytesRead = 0;
                int totalRead = 0;
                while (totalRead < buffer.Length &&
                       (bytesRead = await bodyStream.ReadAsync(buffer, totalRead, buffer.Length - totalRead, cts.Token)) > 0)
                {
                    totalRead += bytesRead;
                }
                var content = System.Text.Encoding.UTF8.GetString(buffer, 0, totalRead);

                if (ContainsErrorPhrases(content))
                    return (false, null);

                // Extra check: booking.com soft 404 has a specific page title
                if (content.Contains("Page Not Found", StringComparison.OrdinalIgnoreCase) ||
                    content.Contains("It happens! Let's get you back on track", StringComparison.OrdinalIgnoreCase) ||
                    content.Contains("<title>Error</title>", StringComparison.OrdinalIgnoreCase))
                    return (false, null);
            }

            return (true, finalGetUrl);
        }
        catch (OperationCanceledException)
        {
            return (false, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Link validation failed for {Url}", url);
            return (false, null);
        }
    }

    public async Task<List<LinkCard>> ValidateAndRepairLinksAsync(List<LinkCard> links, CancellationToken ct = default)
    {
        var results = new List<LinkCard>();
        int replacementCount = 0;

        // Validate all links in parallel (max 3 at a time)
        var validationTasks = links.Select(async link =>
        {
            var (isValid, finalUrl) = await ValidateAsync(link.Url, ct);
            return new { Link = link, IsValid = isValid, FinalUrl = finalUrl };
        }).ToList();

        var validations = await Task.WhenAll(validationTasks);

        foreach (var v in validations)
        {
            if (v.IsValid && !string.IsNullOrWhiteSpace(v.FinalUrl))
            {
                results.Add(new LinkCard { Title = v.Link.Title, Url = v.FinalUrl });
                continue;
            }

            // Link failed validation - try replacement search if under cap
            if (replacementCount < MaxReplacementSearches)
            {
                var replacementUrl = await SearchReplacementUrlAsync(v.Link.Title, ct);
                if (!string.IsNullOrWhiteSpace(replacementUrl))
                {
                    var (repValid, repFinal) = await ValidateAsync(replacementUrl, ct);
                    if (repValid)
                    {
                        results.Add(new LinkCard { Title = v.Link.Title, Url = repFinal ?? replacementUrl });
                        replacementCount++;
                        continue;
                    }
                }
                replacementCount++; // Count attempted replacement even if it failed
            }

            // If replacement failed or cap reached, omit this link (don't show broken URL)
            _logger.LogInformation("Omitting broken link for '{Title}' after validation/replacement failed", v.Link.Title);
        }

        return results;
    }

    private static bool IsRedirect(HttpStatusCode code) =>
        code is HttpStatusCode.Moved or HttpStatusCode.Redirect or
              HttpStatusCode.RedirectMethod or HttpStatusCode.PermanentRedirect or
              HttpStatusCode.TemporaryRedirect;

    private static bool IsSearchPageUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        return SearchPagePatterns.Any(p => p.IsMatch(url));
    }

    private static bool ContainsErrorPhrases(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return false;
        var lower = content.ToLowerInvariant();
        return ErrorPhrases.Any(p => lower.Contains(p.ToLowerInvariant()));
    }

    /// <summary>
    /// Performs a single non-streaming OpenRouter web search to find a replacement URL.
    /// </summary>
    private async Task<string?> SearchReplacementUrlAsync(string placeName, CancellationToken ct)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(20));

            var body = new
            {
                model = "deepseek/deepseek-v4-flash",
                stream = false,
                messages = new object[]
                {
                    new { role = "system", content = "You are a URL finder. Use web_search to find official or direct property pages. Return ONLY JSON: {\"url\":\"https://...\"}. Must not be a search/listing/404 page." },
                    new { role = "user", content = $"Find the official or direct property page for \"{placeName}\". Return ONLY the JSON URL." }
                },
                tools = new object[]
                {
                    new { type = "openrouter:web_search" }
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/chat/completions");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
            request.Headers.Add("HTTP-Referer", "https://tripgenius.online");
            request.Headers.Add("X-Title", "TripGenius");
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cts.Token);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cts.Token);
            using var doc = JsonDocument.Parse(responseJson);

            if (doc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                var content = choices[0].GetProperty("message").GetProperty("content").GetString();
                if (!string.IsNullOrWhiteSpace(content))
                {
                    // Try to extract URL from JSON response - look for "url":"https://..."
                    var urlMatch = Regex.Match(content, "\"url\"\\s*:\\s*\"(https?://[^\"]+)\"");
                    if (urlMatch.Success)
                    {
                        var url = urlMatch.Groups[1].Value.Trim();
                        if (Uri.TryCreate(url, UriKind.Absolute, out _))
                            return url;
                    }

                    // Fallback: find any http URL in the response
                    var anyUrlMatch = Regex.Match(content, "https?://[^\\s<>\"]+");
                    if (anyUrlMatch.Success)
                        return anyUrlMatch.Value.Trim();
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Replacement search failed for '{PlaceName}'", placeName);
            return null;
        }
    }
}
