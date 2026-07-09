using TripGeniusBackend.Domain.Enums;

namespace TripGeniusBackend.Domain.Entities;

public class Message
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string ImageURL { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    // Nullable: AI-authored messages have no real User.
    public User? User { get; set; }
    public int? UserId { get; set; }
    public Trip Trip { get; set; }
    public int TripId { get; set; }
    public SenderType SenderType { get; set; } = SenderType.User;


    protected Message() { }


    private Message(string content, string imageURL, DateTime date, int? userId, int tripId, SenderType senderType)
    {
        Content = content;
        ImageURL = imageURL;
        Date = date;
        UserId = userId;
        TripId = tripId;
        SenderType = senderType;
    }

    public static Message Create(string content, string imageURL, DateTime date, int userId, int tripId)
    {
        Message message = new Message(content, imageURL, date, userId, tripId, SenderType.User);
        return message;
    }

    /// <summary>Creates a message authored by the TripGenius AI agent (no real user).</summary>
    public static Message CreateAi(string content, DateTime date, int tripId) =>
        new Message(content, "", date, null, tripId, SenderType.Ai);
}
