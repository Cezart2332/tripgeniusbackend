namespace WebApplication1.Models;

public class Profile
{
    public int Id { get; set; }
    public string ProfileURL { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
}