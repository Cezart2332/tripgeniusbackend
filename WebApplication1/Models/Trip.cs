namespace WebApplication1.Models;
using WebApplication1.Models.Enums;

public class Trip
{
    public int Id { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public User Owner { get; set; }
    public Status Status { get; set; }
    public List<String> tags { get; set; }
    public int MaxParticipants { get; set; }
    public double Price { get; set; }
 }