namespace TripGeniusBackend.Application.DTOs.User;
public class UserResponse
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string ProfileUrl { get; set; }
    public string Description { get; set; }
    public int GroupSize { get; set; }
    public List<string> Tags { get; set; }
    public string Email { get; set; }
}