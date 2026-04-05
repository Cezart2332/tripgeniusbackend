using WebApplication1.Models.Enums;
namespace WebApplication1.Models;


public class TripHistory
{
    public int Id { get; set; }
    public Trip Trip { get; set; }
    public User User { get; set; }
    public DateTime Date { get; set; }
    public Actions Action { get; set; }
}