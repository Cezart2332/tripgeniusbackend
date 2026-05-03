using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;

namespace TripGeniusBackend.Domain.Entities;

public class User
{
    [Required] public int Id { get; private set; }

    [Required] [EmailAddress] public string Email { get; private set; } = string.Empty;
    [MinLength(8)] public string Password { get; private set; } = string.Empty;

    public string? GoogleId { get; private set; }
    public bool IsVerified { get; private set; } = false;
    public string? VerifyToken { get; private set; }
    public DateTime? VerifyTokenExpires { get; private set; }
    public string? ResetToken { get; private set; }
    public DateTime? ResetTokenExpires { get; private set; }

    public Profile Profile { get; private set; }
    public Preferences Preferences { get; private set; }
    public PushSubscription? PushSubscription { get; private set; }
    private readonly List<Notification> _notifications = new();
    public IReadOnlyCollection<Notification> Notifications => _notifications;
    private readonly List<TripMember> _trips = new();
    public IReadOnlyCollection<TripMember> Trips => _trips;

    protected User()
    {
    }

    public void UpdateGoogleId(string googleId)
    {
        GoogleId = googleId;
    }

    public void VerifyEmail()
    {
        IsVerified = true;
        VerifyToken = null;
        VerifyTokenExpires = null;
    }

    public void ResetPassword()
    {
        ResetToken = null;
        ResetTokenExpires = null;
    }

    public void RegenerateVerifyToken()
    {
        VerifyToken = GenerateToken();
        VerifyTokenExpires = DateTime.UtcNow.AddHours(1);
    }
    public void RegenerateResetToken()
    {
        ResetToken = GenerateToken();
        ResetTokenExpires = DateTime.UtcNow.AddHours(1);
    }

    public void UpdateProfile(string username, string profileURL, string description)
    {
        Profile.Update(username, profileURL, description);
    }
    public void UpdatePreferences(int maxGroupSize, List<String> tags)
    {
        Preferences.Update(maxGroupSize, tags);
    }

    public void UpdateEmail(string newEmail)
    {
        if (!new EmailAddressAttribute().IsValid(newEmail)) throw new ArgumentException("Invalid email address");
        if(newEmail.Equals(Email)) throw new ArgumentException("Email already exists");
        Email = newEmail;
    }

    public void UpdatePassword(string newPassword)
    {
        Password = newPassword;
    }

    public void SubscribeToPush(string endpoint, string p256Dh, string auth)
    {
        PushSubscription = PushSubscription.Create(Id, endpoint, p256Dh, auth);
    }

    public void AddNotification(string content)
    {
        _notifications.Add(Notification.Create(Id, content));
    }

    public void ReadNotification(int notificationId)
    {
        var notification = _notifications.FirstOrDefault(n => n.Id == notificationId);
        if(notification != null) notification.MarkAsRead();
    }

    public void ReadAllNotifications()
    {
        foreach(var notification in _notifications) notification.MarkAsRead();
    }
    
    public static User UserCreate(string email, string password)
    {
        return new User
        {
            Email = email,
            Password = password,
            VerifyToken = GenerateToken(),
            VerifyTokenExpires = DateTime.UtcNow.AddHours(1),
            Profile = new Profile(),
            Preferences = new Preferences()
        };
    }
    public static User UserCreateGoogle(string email, string googleId)
    {
        return new User
        {
            Email = email,
            GoogleId = googleId,
            IsVerified = true,
            Profile = new Profile(),
            Preferences = new Preferences(),
        };
    }
    private static string GenerateToken()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    }
    
}