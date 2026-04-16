using TripGeniusBackend.Domain.Enums;

namespace TripGeniusBackend.Domain.Entities;

public class Bug
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
    public string Description { get; set; } = string.Empty;
    public BugStatus Status { get; set; } = BugStatus.InProgress;
}