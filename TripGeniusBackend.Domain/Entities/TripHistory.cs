using TripGeniusBackend.Domain.Enums;
namespace TripGeniusBackend.Domain.Entities;


public class TripHistory
{
    public int Id { get; set; }
    public int TripId { get; set; }
    public Trip Trip { get; set; }
    public DateTime Date { get; set; }
    public string Content { get; set; } = string.Empty;
    
    protected TripHistory() { }

    public TripHistory( DateTime date, string content)
    {
        Date = date;
        Content = content;
    }
    
}