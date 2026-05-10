using Pgvector;
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
    
    public Vector? Embedding { get; set; }

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
    
    public TripTimeline AddTimeline(int startDay,int endDay, string startingPoint, double[] fromCoords, string endPoint, double[] toCoords, string note)
    {
        var timeline = new TripTimeline(startDay,endDay, startingPoint, fromCoords, endPoint, toCoords, note);
        _timelines.Add(timeline);
        return timeline;
    }
    
    public void RemoveTimeline(int id)
    {
        var timeline = _timelines.FirstOrDefault(t => t.Id == id);
        if(timeline == null) throw new Exception("Timeline not found");
        _timelines.Remove(timeline);
    }

    public void RequestMember(int userId)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if(member != null) throw new Exception("User already in trip");
        _members.Add(new TripMember(userId, Types.Request,Roles.Member, MemberStatus.Requested));
    }

    public void InivteMember(int userId)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if(member != null) throw new Exception("User already in trip");
        _members.Add(new TripMember(userId, Types.Invite,Roles.Member, MemberStatus.Invited));
    }

    public void DeclineMember(int userId)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if(member == null) throw new Exception("User not in trip");
        _members.Remove(member);
    }
    public void AcceptMember(int userId)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if(member == null) throw new Exception("User not in trip");
        member.Accept();
        member.UpdateRole(Roles.Member);
    }

    public void AddOwner(int userId)
    {
        _members.Add(new TripMember(userId, Types.Request, Roles.Owner, MemberStatus.Accepted));
    }
    

    public void RemoveMember(int userId)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if(member == null) throw new Exception("User not in trip");
        _members.Remove(member);
    }

    public void UpdateMemberRole(int userId, Roles role)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if(member == null) throw new Exception("User not in trip");
        member.UpdateRole(role);
    }

    public void AddHistory(string content)
    {
        _histories.Add(new TripHistory(DateTime.UtcNow, content));
    }

    public void SetImageUrl(string imageUrl)
    {
        ImageUrl = imageUrl;
    }

    public void UpdateTrip(string title, string description, DateTime startDate, DateTime endDate, string status,
        List<String> tags, int maxParticipants, double price)
    {
        Title = title;
        Description = description;
        StartDate = startDate;
        EndDate = endDate;
        if (status.Equals("Upcoming")) Status = Status.Upcoming;
        else if(status.Equals("Started")) Status = Status.Started;
        else if(status.Equals("Finished")) Status = Status.Finished;
        Tags = tags;
        MaxParticipants = maxParticipants;
        Price = price;
    }

    public void UpdateTimeline(int id, int newStartDay,int newEndDay, string startingPoint, double[] fromCoords, string endPoint,
        double[] toCoords, string note)
    {
        var timeline = _timelines.FirstOrDefault(t => t.Id == id);   
        timeline.Update(newStartDay,newEndDay, startingPoint, fromCoords, endPoint, toCoords, note);


    }

    public void UpdateEmbedding(Vector embedding)
    {
        Embedding = embedding;
    }

    public static Trip Create(string title, string description, DateTime startDate, DateTime endDate,
        List<String> tags, int maxParticipants, double price,int userId)
    {
        if(string.IsNullOrWhiteSpace(title)) throw new Exception("Title can not be empty");
        if (startDate > endDate) throw new Exception("Invalid Date");
        if (startDate < DateTime.UtcNow) throw new Exception("Trip must be in future");
        var trip = new Trip(title, description, startDate, endDate, tags, maxParticipants, price);
        trip.AddOwner(userId);
        return trip;
    }
}