using System.Globalization;
using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using TripGeniusBackend.API.DTOs;
using TripGeniusBackend.Application.DTOs.Geocoding;
using TripGeniusBackend.Application.DTOs.Trip;

namespace TripGeniusBackend.Infrastructure.Persistence.Services;

public class GeocodingService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;

    public GeocodingService(HttpClient httpClient, IMemoryCache cache)
    {
        _httpClient = httpClient;
        _cache = cache;
    }

    public async Task<List<LocationSuggestion>> SearchAsync(string query, int limit = 6)
    {
        var cacheKey = $"geocoding_{query.ToLower()}_{limit}";
        
        if (_cache.TryGetValue(cacheKey, out List<LocationSuggestion>? cached))
            return cached!;

        var url = $"https://nominatim.openstreetmap.org/search" +
                  $"?q={Uri.EscapeDataString(query)}&format=json&addressdetails=1&limit={limit}";

        var response = await _httpClient.GetFromJsonAsync<List<NominatimResult>>(url);

        var results = (response ?? new())
            .Select(f =>
            {
                if (!double.TryParse(f.Lat, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) ||
                    !double.TryParse(f.Lon, NumberStyles.Float, CultureInfo.InvariantCulture, out var lng))
                    return null;

                var displayName = f.DisplayName ?? "";
                return new LocationSuggestion
                {
                    Id        = f.PlaceId.ToString(),
                    Name      = displayName.Split(',')[0],
                    PlaceName = displayName,
                    Lat       = lat,
                    Lng       = lng
                };
            })
            .Where(x => x != null)
            .Select(x => x!)
            .ToList();

        _cache.Set(cacheKey, results, TimeSpan.FromHours(24));

        return results;
    }

    /// <summary>
    /// Terrain elevation lookup via OpenTopoData (server-side; no browser CORS).
    /// </summary>
    public async Task<List<double>> LookupElevationsAsync(
        IReadOnlyList<ElevationPointDto> points,
        CancellationToken cancellationToken = default)
    {
        if (points.Count == 0)
            return [];

        const int chunkSize = 100;
        var elevations = new double[points.Count];
        Array.Fill(elevations, double.NaN);

        for (var offset = 0; offset < points.Count; offset += chunkSize)
        {
            var chunk = points.Skip(offset).Take(chunkSize).ToList();
            var locations = string.Join("|", chunk.Select(p =>
                $"{p.Lat.ToString(CultureInfo.InvariantCulture)},{p.Lng.ToString(CultureInfo.InvariantCulture)}"));

            var url =
                $"https://api.opentopodata.org/v1/aster30m?locations={Uri.EscapeDataString(locations)}";

            var response = await _httpClient.GetFromJsonAsync<OpenTopoDataResponse>(url, cancellationToken)
                ?? throw new InvalidOperationException("Elevation service returned no data.");

            if (!string.Equals(response.Status, "OK", StringComparison.OrdinalIgnoreCase) ||
                response.Results == null)
                throw new InvalidOperationException("Elevation lookup failed.");

            for (var j = 0; j < response.Results.Count && offset + j < elevations.Length; j++)
            {
                var elev = response.Results[j].Elevation;
                elevations[offset + j] = elev ?? double.NaN;
            }

            if (offset + chunkSize < points.Count)
                await Task.Delay(1100, cancellationToken);
        }

        if (elevations.Count(e => !double.IsNaN(e)) < 2)
            throw new InvalidOperationException("Not enough elevation data for this route.");

        return elevations.ToList();
    }

    private sealed class OpenTopoDataResponse
    {
        public string Status { get; set; } = "";
        public List<OpenTopoResult>? Results { get; set; }
    }

    private sealed class OpenTopoResult
    {
        public double? Elevation { get; set; }
    }
}


