namespace TripGeniusBackend.Application.DTOs.Trip;

public class TripTimelineResponse
{
    public int Id { get; set; }
    public int Day { get; set; }
    public string StartingPoint { get; set; } = string.Empty;
    public string EndPoint { get; set; } = string.Empty;
    public double[] FromCoords { get; set; } = new double[2];

    public double[] ToCoords { get; set; } = new double[2];
    public string Note {get;set;}
}