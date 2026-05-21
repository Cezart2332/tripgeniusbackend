using TripGeniusBackend.Application.Interfaces.Services;
using TripGeniusBackend.Infrastructure.Persistence.Services;
using Xunit;
using FluentAssertions;

namespace TripGeniusBackend.Tests.Unit.Services;

public class LinkValidationServiceTests
{
    private readonly LinkValidationService _service = new();

    [Fact]
    public async Task ValidateAsync_WithEmptyUrl_ReturnsInvalid()
    {
        var (isValid, finalUrl) = await _service.ValidateAsync("");
        isValid.Should().BeFalse();
        finalUrl.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_WithValidHotelUrl_ReturnsValid()
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
    public async Task ValidateAndRepairLinksAsync_FiltersMomondoCityListing()
    {
        var links = new List<LinkCard>
        {
            new() { Title = "X", Url = "https://www.momondo.com/hotels/brasov-vacation-rentals-17704.ksp" },
            new() { Title = "Y", Url = "https://www.booking.com/hotel/ro/real.html" },
        };

        var result = await _service.ValidateAndRepairLinksAsync(links);
        result.Should().HaveCount(1);
        result[0].Url.Should().Contain("booking.com/hotel");
    }
}
