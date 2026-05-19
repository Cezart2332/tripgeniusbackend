namespace TripGeniusBackend.Application.Interfaces.Services;

public interface ILinkValidationService
{
    /// <summary>
    /// Validates a single URL. Returns (isValid, finalUrl) tuple.
    /// finalUrl may differ from input due to redirects.
    /// </summary>
    Task<(bool isValid, string? finalUrl)> ValidateAsync(string url, CancellationToken ct = default);

    /// <summary>
    /// Validates a list of links and repairs broken ones using web search.
    /// Caps replacement searches at 2 per batch to avoid tail latency.
    /// </summary>
    Task<List<LinkCard>> ValidateAndRepairLinksAsync(List<LinkCard> links, CancellationToken ct = default);
}

public class LinkCard
{
    public required string Title { get; set; }
    public required string Url { get; set; }
}
