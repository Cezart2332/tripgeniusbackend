using TripGeniusBackend.Domain.Enums;
namespace TripGeniusBackend.Domain.Entities;

public class TripMember
{
    public int Id { get; private set; }
    public int UserId { get; private set; }
    public User User { get; private set; }
    public int TripId { get; private set; }
    public Trip Trip { get; private set; }
    public Roles Role { get; private set; }
    protected TripMember() { }

    public TripMember(int userId, Roles role)
    {
        UserId = userId;
        Role = role;
    }
}