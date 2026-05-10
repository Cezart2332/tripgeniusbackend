using TripGeniusBackend.Domain.Enums;

namespace TripGeniusBackend.Domain.Entities;

public class TripTimeline
{
    public int Id { get; private set; }
    public int StartDay{get;private set;}
    public int EndDay{get;private set;}
    public Trip Trip { get; private set; }
    public string StartingPoint { get; private set; } = string.Empty;
    public double[] FromCoords { get; private set; } = new double[2];
    public string EndPoint { get; private set; } = string.Empty;
    public double[] ToCoords { get; private set; } = new double[2];
    public string Note {get;private set;}
    private readonly List<TripActivity> _activities = new();
    public IReadOnlyCollection<TripActivity> Activities => _activities;
    
    protected TripTimeline(){}

    public void UpdateDay(int startDay,int endDay)
    {
        StartDay = startDay;
        EndDay = endDay;
    }

    public void Update(int startDay,int endDay, string startingPoint, double[] fromCoords, string endPoint, double[] toCoords,
        string note)
    {
        StartDay = startDay;
        EndDay = endDay;
        StartingPoint = startingPoint;
        FromCoords = fromCoords;
        EndPoint = endPoint;
        ToCoords = toCoords;
        Note = note;
    }

    public void AddActivity(TripActivity activity)
    {
        _activities.Add(activity);
    }
    public void RemoveActivity(TripActivity activity)
    {
        _activities.Remove(activity);
    }

    public void UpdateActivity(int id,string name,string description, string link, double cost, ActivityType type)
    {
        var activity = _activities.FirstOrDefault(a => a.Id == id);
        if(activity == null) throw new Exception("Activity not found");
        activity.Update(name,description,link,cost,type);
    }
    
    public TripTimeline(int startDay,int endDay, string startingPoint, double[] fromCoords, string endPoint, double[] toCoords, string note)
    {
        StartDay = startDay;
        EndDay = endDay;
        StartingPoint = startingPoint;
        FromCoords = fromCoords;
        EndPoint = endPoint;
        ToCoords = toCoords;
        Note = note;
    }
    
}