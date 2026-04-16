namespace TripGeniusBackend.Domain.Entities;

public class TripCost
{
    public int Id { get; set; }
    public Trip Trip { get; set; }
    public double Cost { get; set; }
    public string Description { get; set; } = string.Empty;
}