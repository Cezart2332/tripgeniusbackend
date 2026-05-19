namespace TripGeniusBackend.Domain.Entities;

public class OffroadMessage
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string ImageURL { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public User User { get; set; } = null!;
    public int UserId { get; set; }
    public OffroadTrip OffroadTrip { get; set; } = null!;
    public int OffroadTripId { get; set; }

    protected OffroadMessage() { }

    private OffroadMessage(string content, string imageUrl, DateTime date, int userId, int offroadTripId)
    {
        Content = content;
        ImageURL = imageUrl;
        Date = date;
        UserId = userId;
        OffroadTripId = offroadTripId;
    }

    public static OffroadMessage Create(string content, string imageUrl, DateTime date, int userId, int offroadTripId) =>
        new(content, imageUrl, date, userId, offroadTripId);
}
