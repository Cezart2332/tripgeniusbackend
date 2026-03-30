namespace WebApplication1.Models;

public class RefreshToken
{
    public int Id { get; set; }
    public string Token { get; set; }
    public User User { get; set; }
    public DateTime Expires { get; set; }
}