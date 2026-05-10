using System.Text.Json.Serialization;

namespace TripGeniusBackend.Application.DTOs.Trip;

public class NominatimResult
{
    [JsonPropertyName("place_id")]
    public long PlaceId { get; set; }

    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("lat")]
    public string Lat { get; set; } = string.Empty;

    [JsonPropertyName("lon")]
    public string Lon { get; set; } = string.Empty;
}