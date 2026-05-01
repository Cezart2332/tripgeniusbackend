namespace TripGeniusBackend.Domain.Entities;

public class Notification
{
    public int Id {get; private set;}
    public int UserId {get; private set;}
    public User User { get; private set;}
    public string Content {get; private set;}
    public DateTime CreatedAt {get; private set;}
    public bool IsRead {get; private set;}
    
    protected Notification() { }
    private Notification(int userId, string content)
    {
        UserId = userId;
        Content = content;
        CreatedAt = DateTime.UtcNow;
        IsRead = false;
    }

    public void MarkAsRead()
    {
        IsRead = true;
    }

    public static Notification Create(int userId, string content)
    {
        Notification notification = new Notification(userId, content);
        return notification;
    }
}