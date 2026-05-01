namespace TripGeniusBackend.Domain.Entities;

public class Message
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string ImageURL { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public User User { get; set; }
    public int UserId { get; set; }
    public Trip Trip { get; set; }
    public int TripId { get; set; }
    
    
    protected Message() { }


    private Message(string content, string imageURL, DateTime date, int userId, int tripId)
    {
        Content = content;
        ImageURL = imageURL;
        Date = date;
        UserId = userId;
        TripId = tripId;
    }

    public static Message Create(string content, string imageURL, DateTime date, int userId, int tripId)
    {
        Message message = new Message(content, imageURL, date, userId, tripId);
        return message;
    }
}