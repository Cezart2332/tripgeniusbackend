using TripGeniusBackend.Domain.Enums;

namespace TripGeniusBackend.Application.DTOs.Trip;

public class MemberResponse
{
    public int TripId { get; set; }
    public int InvitedId { get; set; }
    public string MemberStatus { get; set; }
    public string Action { get; set; } = string.Empty;
}