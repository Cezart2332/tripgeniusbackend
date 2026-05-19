using FluentAssertions;
using TripGeniusBackend.Domain.Entities;
using TripGeniusBackend.Domain.Enums;
using TripGeniusBackend.Infrastructure.Persistence.Services;
using Xunit;

namespace TripGeniusBackend.Tests.Unit.Services;

public class GpxServiceTests
{
    private readonly GpxService _service = new();

  private const string SampleGpx = """
        <?xml version="1.0" encoding="UTF-8"?>
        <gpx version="1.1" creator="test">
          <trk><trkseg>
            <trkpt lat="45.44" lon="25.33"><ele>100</ele></trkpt>
            <trkpt lat="45.45" lon="25.34"><ele>110</ele></trkpt>
            <trkpt lat="45.46" lon="25.35"><ele>120</ele></trkpt>
          </trkseg></trk>
        </gpx>
        """;

    [Fact]
    public async Task ParseGpxAsync_ReturnsLineStringAndDistance()
    {
        await using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(SampleGpx));
        var result = await _service.ParseGpxAsync(stream);

        result.TrackGeoJson.Should().Contain("LineString");
        result.DistanceMeters.Should().BeGreaterThan(0);
        result.OriginalGpx.Should().Contain("trkpt");
    }

    [Fact]
    public void BuildRouteGpx_ProducesValidXml()
    {
        var route = new OffroadRoute(1, 1, "Test", "", """{"type":"LineString","coordinates":[[25.33,45.44],[25.35,45.46]]}""",
            RouteSource.Drawn, 1000, 20);
        var bytes = _service.BuildRouteGpx(route, "Trip");
        var xml = System.Text.Encoding.UTF8.GetString(bytes);
        xml.Should().Contain("<gpx");
        xml.Should().Contain("trkpt");
    }
}
