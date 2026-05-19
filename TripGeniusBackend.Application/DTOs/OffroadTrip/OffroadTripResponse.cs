using TripGeniusBackend.Application.DTOs.Trip;

namespace TripGeniusBackend.Application.DTOs.OffroadTrip;

public class OffroadTripResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartingDate { get; set; }
    public DateTime EndingDate { get; set; }
    public double Price { get; set; }
    public int CurrentMembers { get; set; }
    public int MaxParticipants { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<OffroadRouteResponse> Routes { get; set; } = new();
    public List<TripMemberResponse> Members { get; set; } = new();
    public List<TripHistoryResponse> History { get; set; } = new();
    public bool IsUserMember { get; set; }
}
