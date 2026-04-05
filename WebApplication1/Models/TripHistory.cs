namespace WebApplication1.Models;
using WebApplication1.Models.Enums;

public class TripHistory
{
    public Trip Trip { get; set; }
    public User User { get; set; }
    public DateTime Date { get; set; }
    public Actions Action { get; set; }
}