
namespace TripGeniusBackend.Application.DTOs.Trip;


public class TripResponse
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string ImageUrl { get; set; }
    public string Status { get; set; }
    public DateTime StartingDate { get; set; }
    public DateTime EndingDate { get; set; }
    public double Price { get; set; }
    public int CurrentMembers { get; set; }
    public int MaxParticipants { get; set; }
    public List<string> Tags { get; set; }
    public List<TripTimelineRequest?> Timelines { get; set; }
    public List<TripMemberResponse?> Members { get; set; }
    public bool isUserMember { get; set; }
}