namespace TripGeniusBackend.Domain.Entities;

public class OffroadTripHistory
{
    public int Id { get; set; }
    public int OffroadTripId { get; set; }
    public OffroadTrip OffroadTrip { get; set; } = null!;
    public DateTime Date { get; set; }
    public string Content { get; set; } = string.Empty;

    protected OffroadTripHistory() { }

    public OffroadTripHistory(DateTime date, string content)
    {
        Date = date;
        Content = content;
    }
}
