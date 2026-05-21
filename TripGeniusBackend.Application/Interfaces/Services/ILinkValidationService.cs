namespace TripGeniusBackend.Application.Interfaces.Services;

public interface ILinkValidationService
{
    /// <summary>Basic URL shape / listing-page check. Live verification is done by the model via web_fetch.</summary>
    Task<(bool isValid, string? finalUrl)> ValidateAsync(string url, CancellationToken ct = default);

    /// <summary>Filters invalid, duplicate, and obvious city-listing URLs from the model's [LINKS] block.</summary>
    Task<List<LinkCard>> ValidateAndRepairLinksAsync(List<LinkCard> links, CancellationToken ct = default);
}

public class LinkCard
{
    public required string Title { get; set; }
    public required string Url { get; set; }
}
