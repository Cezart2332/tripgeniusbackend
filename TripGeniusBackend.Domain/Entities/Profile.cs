namespace TripGeniusBackend.Domain.Entities;

public class Profile
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    public User User { get; set; }
    public string ProfileURL { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Username { get; private set; } = string.Empty;
    
    public void Update(string username, string profileURL, string description)
    {
        Username = username;
        ProfileURL = profileURL;
        Description = description;
    }
}