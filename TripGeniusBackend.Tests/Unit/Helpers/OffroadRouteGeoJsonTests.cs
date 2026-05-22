using FluentAssertions;
using TripGeniusBackend.Application.Helpers;
using Xunit;

namespace TripGeniusBackend.Tests.Unit.Helpers;

public class OffroadRouteGeoJsonTests
{
    [Fact]
    public void NormalizeForStorage_EmptyString_ReturnsValidLineString()
    {
        var result = OffroadRouteGeoJson.NormalizeForStorage("");
        result.Should().Be(OffroadRouteGeoJson.EmptyLineString);
    }

    [Fact]
    public void NormalizeForStorage_ValidJson_ReturnsTrimmed()
    {
        const string geoJson = "  {\"type\":\"LineString\",\"coordinates\":[[1,2],[3,4]]}  ";
        OffroadRouteGeoJson.NormalizeForStorage(geoJson).Should().Be(geoJson.Trim());
    }

    [Fact]
    public void NormalizeForStorage_InvalidJson_Throws()
    {
        var act = () => OffroadRouteGeoJson.NormalizeForStorage("not-json");
        act.Should().Throw<ArgumentException>();
    }
}
