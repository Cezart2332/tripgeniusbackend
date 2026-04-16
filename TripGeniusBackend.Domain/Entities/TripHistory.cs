using TripGeniusBackend.Domain.Enums;
namespace TripGeniusBackend.Domain.Entities;


public class TripHistory
{
    public int Id { get; set; }
    public Trip Trip { get; set; }
    public User User { get; set; }
    public DateTime Date { get; set; }
    public Actions Action { get; set; }
}