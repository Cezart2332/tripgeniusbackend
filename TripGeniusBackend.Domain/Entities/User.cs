using System.ComponentModel.DataAnnotations;

namespace TripGeniusBackend.Domain.Entities;

public class User
{
    [Required]
    public int Id { get; private set; }
    
    [Required]
    [EmailAddress]
    public string Email { get; private set; } = string.Empty;
    [Required]
    [MinLength(8)]
    public string Password { get; private set; } = string.Empty;
    
    public Profile Profile { get; private set; }
    public Preferences Preferences { get; private set; }
    
    protected User(){}

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
    
    private User(string email, string password)
    {
        Email = email;
        Password = password;
        Profile = new Profile();
        Preferences = new Preferences();
    }

    public static User UserCreate(string email, string password)
    {
        var User = new User(email, password);
        return User;
    }
    
}