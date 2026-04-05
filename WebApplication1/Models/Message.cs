namespace WebApplication1.Models;

public class Message
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string ImageURL { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public User Sender { get; set; }
    public Trip Trip { get; set; }
}