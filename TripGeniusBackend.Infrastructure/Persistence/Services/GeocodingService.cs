using System.Globalization;
using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using TripGeniusBackend.API.DTOs;
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

        var results = response?.Select(f => new LocationSuggestion
        {
            Id       = f.PlaceId.ToString(),
            Name     = f.DisplayName.Split(',')[0],
            PlaceName = f.DisplayName,
            Lat      = double.Parse(f.Lat, CultureInfo.InvariantCulture),
            Lng      = double.Parse(f.Lon, CultureInfo.InvariantCulture)
        }).ToList() ?? new();

        _cache.Set(cacheKey, results, TimeSpan.FromHours(24));

        return results;
    }
}


