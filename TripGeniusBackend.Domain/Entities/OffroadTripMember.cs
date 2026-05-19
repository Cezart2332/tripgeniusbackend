using TripGeniusBackend.Domain.Enums;

namespace TripGeniusBackend.Domain.Entities;

public class OffroadTripMember
{
    public int Id { get; private set; }
    public Types Type { get; private set; }
    public int UserId { get; private set; }
    public User User { get; private set; } = null!;
    public int OffroadTripId { get; private set; }
    public OffroadTrip OffroadTrip { get; private set; } = null!;
    public Roles Role { get; private set; }
    public MemberStatus MemberStatus { get; private set; }

    protected OffroadTripMember() { }

    public void UpdateRole(Roles role) => Role = role;

    public void Accept() => MemberStatus = MemberStatus.Accepted;

    public OffroadTripMember(int userId, Types type, Roles role, MemberStatus memberStatus = MemberStatus.Invited)
    {
        UserId = userId;
        Type = type;
        Role = role;
        MemberStatus = memberStatus;
    }
}
