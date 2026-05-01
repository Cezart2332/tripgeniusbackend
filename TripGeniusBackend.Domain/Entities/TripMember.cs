using TripGeniusBackend.Domain.Enums;
namespace TripGeniusBackend.Domain.Entities;

public class TripMember
{
    public int Id { get; private set; }
    public Types Type { get; private set; }
    public int UserId { get; private set; }
    public User User { get; private set; }
    public int TripId { get; private set; }
    public Trip Trip { get; private set; }
    public int InvitedBy { get; private set; }
    public Roles Role { get; private set; }
    public MemberStatus MemberStatus { get; private set; }
    
    protected TripMember() { }

    public void UpdateRole(Roles role)
    {
        Role = role;
    }

    public void Accept()
    {
        MemberStatus = MemberStatus.Accepted;
    }

    public TripMember(int userId,Types type ,Roles role,MemberStatus memberStatu = MemberStatus.Invited)
    {
        UserId = userId;
        Type = type;
        Role = role;
        MemberStatus = memberStatu;
    }
}