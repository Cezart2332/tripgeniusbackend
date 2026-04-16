namespace TripGeniusBackend.Domain.Entities;

public class RefreshToken
{
    public int Id { get; set; }
    public string Token { get; set; }
    
    public int UserId { get; set; }
    public User User { get; set; }
    public DateTime Expires { get; set; }
}