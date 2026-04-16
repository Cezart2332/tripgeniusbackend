namespace TripGeniusBackend.Domain.Entities;

public class TripTimeline
{
    public int Id { get; private set; }
    public int Day{get;private set;}
    public Trip Trip { get; private set; }
    public string StartingPoint { get; private set; } = string.Empty;
    public double[] FromCoords { get; private set; } = new double[2];
    public string EndPoint { get; private set; } = string.Empty;
    public double[] ToCoords { get; private set; } = new double[2];
    public string Note {get;private set;}
    
    protected TripTimeline(){}
    
    public TripTimeline(int day, string startingPoint, double[] fromCoords, string endPoint, double[] toCoords, string note)
    {
        Day = day;
        StartingPoint = startingPoint;
        FromCoords = fromCoords;
        EndPoint = endPoint;
        ToCoords = toCoords;
        Note = note;
    }
    
}