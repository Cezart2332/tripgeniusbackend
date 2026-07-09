namespace TripGeniusBackend.Application.Interfaces.Services;

public interface ILinkValidationService
{
    /// <summary>
    /// Live-checks a single URL over HTTP: drops it if the page is gone (404/410), the host does not
    /// exist, or it resolves to a search / city-listing page. Returns the URL to keep on success.
    /// </summary>
    Task<(bool isValid, string? finalUrl)> ValidateAsync(string url, CancellationToken ct = default);

    /// <summary>
    /// Validates the model's [LINKS] block in parallel. Dead / search-page links are repaired: first via
    /// the optional <paramref name="reSearch"/> callback (a fresh model web_search for that place), then,
    /// if that also fails, with a precise Google Maps link. Duplicates are removed.
    /// </summary>
    Task<List<LinkCard>> ValidateAndRepairLinksAsync(
        List<LinkCard> links,
        Func<LinkCard, Task<string?>>? reSearch = null,
        CancellationToken ct = default);
}

public class LinkCard
{
    public required string Title { get; set; }
    public required string Url { get; set; }
}
