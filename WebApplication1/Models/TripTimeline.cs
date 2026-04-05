namespace WebApplication1.Models;

public class TripTimeline
{
    public int Id { get; set; }
    public Trip Trip { get; set; }
    public string StartingPoint { get; set; } = string.Empty;
    public string EndPoint { get; set; } = string.Empty;
    public int Day{get;set;}
}