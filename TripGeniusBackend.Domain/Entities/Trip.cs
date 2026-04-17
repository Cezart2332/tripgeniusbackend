using TripGeniusBackend.Domain.Enums;
namespace TripGeniusBackend.Domain.Entities;


public class Trip
{
    public int Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public string ImageUrl { get; private set; } = string.Empty;
    public Status Status { get; private set; }
    public List<String> Tags { get; private set; }
    
    public int MaxParticipants { get; private set; }
    public double Price { get; private set; }

    private readonly List<TripTimeline> _timelines = new();
    public IReadOnlyCollection<TripTimeline> Timelines => _timelines;
    
    private readonly List<TripMember> _members = new();
    public IReadOnlyCollection<TripMember> Members => _members;
    private readonly List<TripHistory> _histories = new();
    public IReadOnlyCollection<TripHistory> History => _histories;

    protected Trip() {}

    private Trip(string title, string description, DateTime startDate, DateTime endDate, List<String> tags, int maxParticipants, double price)
    {
        Title = title;
        Description = description;
        StartDate = startDate;
        EndDate = endDate;
        Status = Status.Upcoming;
        Tags = tags;
        MaxParticipants = maxParticipants;
        Price = price;
    }
    
    public void AddTimeline(int day, string startingPoint, double[] fromCoords, string endPoint, double[] toCoords, string note)
    {
        _timelines.Add(new TripTimeline(day, startingPoint, fromCoords, endPoint, toCoords, note));
    }
    

    public void AddMember(int userId, Roles role)
    {
        if(_members.Count >= MaxParticipants) throw new Exception("Trip is full");
        if(_members.Any(m => m.UserId == userId)) throw new Exception("User already in trip");
        _members.Add(new TripMember(userId, role));
    }

    public void SetImageUrl(string imageUrl)
    {
        ImageUrl = imageUrl;
    }
    
    public static Trip Create(string title, string description, DateTime startDate, DateTime endDate,
        List<String> tags, int maxParticipants, double price,int userId)
    {
        if(string.IsNullOrWhiteSpace(title)) throw new Exception("Title can not be empty");
        if (startDate >= endDate) throw new Exception("Invalid Date");
        if (startDate < DateTime.UtcNow) throw new Exception("Trip must be in future");
        var trip = new Trip(title, description, startDate, endDate, tags, maxParticipants, price);
        trip.AddMember(userId,Roles.Owner);
        return trip;
    }
}