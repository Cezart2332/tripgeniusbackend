using TripGeniusBackend.Domain.Enums;

namespace TripGeniusBackend.Domain.Entities;

public class OffroadMessage
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string ImageURL { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    // Nullable: AI-authored messages have no real User.
    public User? User { get; set; }
    public int? UserId { get; set; }
    public OffroadTrip OffroadTrip { get; set; } = null!;
    public int OffroadTripId { get; set; }
    public SenderType SenderType { get; set; } = SenderType.User;

    protected OffroadMessage() { }

    private OffroadMessage(string content, string imageUrl, DateTime date, int? userId, int offroadTripId, SenderType senderType)
    {
        Content = content;
        ImageURL = imageUrl;
        Date = date;
        UserId = userId;
        OffroadTripId = offroadTripId;
        SenderType = senderType;
    }

    public static OffroadMessage Create(string content, string imageUrl, DateTime date, int userId, int offroadTripId) =>
        new(content, imageUrl, date, userId, offroadTripId, SenderType.User);

    /// <summary>Creates a message authored by the TripGenius AI agent (no real user).</summary>
    public static OffroadMessage CreateAi(string content, DateTime date, int offroadTripId) =>
        new(content, "", date, null, offroadTripId, SenderType.Ai);
}
