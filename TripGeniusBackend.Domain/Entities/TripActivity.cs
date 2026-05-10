using TripGeniusBackend.Domain.Enums;

namespace TripGeniusBackend.Domain.Entities;

public class TripActivity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Link { get; set; } = string.Empty;
    public double? Cost { get; set; }
    public ActivityType Type { get; set; }
    public TripTimeline TripTimeline { get; set; }
    public int TripTimelineId { get; set; }
    
    
    public TripActivity(string name, string description, string? link, double? cost, ActivityType type)
    {
        Name = name;
        Description = description;
        Link = link;
        Cost = cost;
        Type = type;
    }
    
    public void Update(string name, string description, string? link, double? cost,  ActivityType type)
    {
        Name = name;
        Description = description;
        Link = link;
        Cost = cost;
        Type = type;
    }
}