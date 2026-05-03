namespace TripGeniusBackend.Domain.Entities;

public class PushSubscription
{
    public int Id { get; private set; }
    public User User { get; private set; }
    public int UserId { get; private set; }
    public string Endpoint { get; private set; } = string.Empty;
    public string P256dh { get; private set; } = string.Empty;
    public string Auth { get; private set; } = string.Empty;
    
    protected PushSubscription() { }

    private PushSubscription(int userId, string endpoint, string p256Dh, string auth)
    {
        UserId = userId;
        Endpoint = endpoint;
        P256dh = p256Dh;
        Auth = auth;
    }

    public static PushSubscription Create(int userId , string endpoint, string p256Dh, string auth)
    {
        return new PushSubscription(userId, endpoint, p256Dh, auth);
    }
}