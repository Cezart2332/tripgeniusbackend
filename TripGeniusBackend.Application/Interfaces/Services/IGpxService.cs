using TripGeniusBackend.Application.DTOs.OffroadTrip;
using TripGeniusBackend.Domain.Entities;

namespace TripGeniusBackend.Application.Interfaces.Services;

public interface IGpxService
{
    Task<GpxParseResult> ParseGpxAsync(Stream stream, CancellationToken cancellationToken = default);
    byte[] BuildRouteGpx(OffroadRoute route, string tripTitle);
    byte[] BuildTripGpx(OffroadTrip trip);
}
