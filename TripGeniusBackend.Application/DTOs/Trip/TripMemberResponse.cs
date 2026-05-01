
using TripGeniusBackend.Domain.Enums;

namespace TripGeniusBackend.Application.DTOs.Trip;

public class TripMemberResponse
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Role { get; set; }
    public string AvatarUrl { get; set; }
    public string MemberStatus { get; set; } 
}