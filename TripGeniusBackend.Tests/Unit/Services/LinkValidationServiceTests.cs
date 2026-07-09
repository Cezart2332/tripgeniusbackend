using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using TripGeniusBackend.Application.Interfaces.Services;
using TripGeniusBackend.Infrastructure.Persistence.Services;
using Xunit;
using FluentAssertions;

namespace TripGeniusBackend.Tests.Unit.Services;

public class LinkValidationServiceTests
{
    /// <summary>Stub transport so tests never hit the network; the responder decides the outcome.</summary>
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responder = responder;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = _responder(request);
            response.RequestMessage ??= request;
            return Task.FromResult(response);
        }
    }

    private static LinkValidationService CreateService(Func<HttpRequestMessage, HttpResponseMessage>? responder = null)
    {
        responder ??= _ => new HttpResponseMessage(HttpStatusCode.OK);
        var client = new HttpClient(new StubHandler(responder));
        return new LinkValidationService(client, NullLogger<LinkValidationService>.Instance);
    }

    private readonly LinkValidationService _service = CreateService();

    [Fact]
    public async Task ValidateAsync_WithEmptyUrl_ReturnsInvalid()
    {
        var (isValid, finalUrl) = await _service.ValidateAsync("");
        isValid.Should().BeFalse();
        finalUrl.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_WithLiveHotelUrl_ReturnsValid()
    {
        var url = "https://www.booking.com/hotel/ro/test-hotel.html";
        var (isValid, finalUrl) = await _service.ValidateAsync(url);
        isValid.Should().BeTrue();
        finalUrl.Should().Be(url);
    }

    [Fact]
    public async Task ValidateAsync_WithCityListingUrl_ReturnsInvalid()
    {
        var (isValid, _) = await _service.ValidateAsync("https://www.booking.com/city/ro/brasov.html");
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_WhenPageReturns404_ReturnsInvalid()
    {
        var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var (isValid, _) = await service.ValidateAsync("https://www.booking.com/hotel/ro/does-not-exist.html");
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_WhenRedirectedToSearchPage_ReturnsInvalid()
    {
        // Simulate a dead property page that 301s to a search-results page.
        var service = CreateService(req => new HttpResponseMessage(HttpStatusCode.OK)
        {
            RequestMessage = new HttpRequestMessage(HttpMethod.Get,
                "https://www.booking.com/searchresults.html?ss=brasov")
        });
        var (isValid, _) = await service.ValidateAsync("https://www.booking.com/hotel/ro/moved.html");
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_WhenBlockedByAntiBot403_KeepsLink()
    {
        var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.Forbidden));
        var url = "https://www.booking.com/hotel/ro/real-but-protected.html";
        var (isValid, _) = await service.ValidateAsync(url);
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_WithGoogleMapsLink_SkipsNetworkAndReturnsValid()
    {
        // Responder throws to prove Google Maps fallbacks are accepted without a live probe.
        var service = CreateService(_ => throw new HttpRequestException("should not be called"));
        var url = "https://www.google.com/maps/search/?api=1&query=Hotel+Brasov";
        var (isValid, _) = await service.ValidateAsync(url);
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAndRepairLinksAsync_RemovesDuplicates()
    {
        var links = new List<LinkCard>
        {
            new() { Title = "A", Url = "https://example.com/hotel" },
            new() { Title = "B", Url = "https://example.com/hotel" },
        };

        var result = await _service.ValidateAndRepairLinksAsync(links);
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task ValidateAndRepairLinksAsync_RepairsMomondoCityListingWithMapFallback()
    {
        var links = new List<LinkCard>
        {
            new() { Title = "Casa Wagner, Brașov", Url = "https://www.momondo.com/hotels/brasov-vacation-rentals-17704.ksp" },
            new() { Title = "Y", Url = "https://www.booking.com/hotel/ro/real.html" },
        };

        var result = await _service.ValidateAndRepairLinksAsync(links);

        // The place is kept, but its listing URL is replaced with a precise map link.
        result.Should().HaveCount(2);
        result.Should().Contain(l => l.Url.Contains("booking.com/hotel"));
        result.Should().Contain(l => l.Title == "Casa Wagner, Brașov"
                                     && l.Url.Contains("google.com/maps"));
    }

    [Fact]
    public async Task ValidateAndRepairLinksAsync_UsesReSearchResult_WhenItIsLive()
    {
        // The original link 404s; the re-searched replacement is live and should be used.
        var service = CreateService(req =>
            req.RequestUri!.AbsoluteUri.Contains("dead")
                ? new HttpResponseMessage(HttpStatusCode.NotFound)
                : new HttpResponseMessage(HttpStatusCode.OK));

        var links = new List<LinkCard>
        {
            new() { Title = "Hotel X, Cluj", Url = "https://www.booking.com/hotel/ro/dead.html" },
        };

        var result = await service.ValidateAndRepairLinksAsync(
            links,
            reSearch: _ => Task.FromResult<string?>("https://www.booking.com/hotel/ro/fresh.html"));

        result.Should().ContainSingle();
        result[0].Url.Should().Contain("fresh");
    }

    [Fact]
    public async Task ValidateAndRepairLinksAsync_FallsBackToMap_WhenReSearchResultAlsoDead()
    {
        // Everything 404s, including the re-search result → deterministic map fallback.
        var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var links = new List<LinkCard>
        {
            new() { Title = "Hotel X, Cluj", Url = "https://www.booking.com/hotel/ro/dead.html" },
        };

        var result = await service.ValidateAndRepairLinksAsync(
            links,
            reSearch: _ => Task.FromResult<string?>("https://www.booking.com/hotel/ro/also-dead.html"));

        result.Should().ContainSingle();
        result[0].Url.Should().Contain("google.com/maps");
    }

    [Fact]
    public async Task ValidateAndRepairLinksAsync_RepairsDeadLinksWithMapFallback()
    {
        var service = CreateService(req =>
            req.RequestUri!.AbsoluteUri.Contains("dead")
                ? new HttpResponseMessage(HttpStatusCode.NotFound)
                : new HttpResponseMessage(HttpStatusCode.OK));

        var links = new List<LinkCard>
        {
            new() { Title = "Dead Place", Url = "https://www.booking.com/hotel/ro/dead.html" },
            new() { Title = "Live", Url = "https://www.booking.com/hotel/ro/live.html" },
        };

        var result = await service.ValidateAndRepairLinksAsync(links);

        result.Should().HaveCount(2);
        result.Single(l => l.Title == "Live").Url.Should().Contain("booking.com/hotel/ro/live");
        result.Single(l => l.Title == "Dead Place").Url.Should().Contain("google.com/maps");
    }
}
