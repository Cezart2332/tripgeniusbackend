namespace WebApplication1.Models;

public class Preferences
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    public User User { get; set; }
    public int MaxGroupSize { get; set; }
    public List<String> Tags { get; set; }
    
}