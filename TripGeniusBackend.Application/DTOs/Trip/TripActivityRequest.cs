using TripGeniusBackend.Domain.Enums;

namespace TripGeniusBackend.Application.DTOs.Trip;

public class TripActivityRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Link { get; set; } = string.Empty;
    public double? Cost { get; set; }
    public ActivityType Type { get; set; }
}