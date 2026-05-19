using Pgvector;
using TripGeniusBackend.Domain.Enums;

namespace TripGeniusBackend.Domain.Entities;

public class OffroadTrip
{
    public int Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public string ImageUrl { get; private set; } = string.Empty;
    public Status Status { get; private set; }
    public List<string> Tags { get; private set; } = new();
    public int MaxParticipants { get; private set; }
    public double Price { get; private set; }
    public Vector? Embedding { get; set; }

    private readonly List<OffroadRoute> _routes = new();
    public IReadOnlyCollection<OffroadRoute> Routes => _routes;

    private readonly List<OffroadTripMember> _members = new();
    public IReadOnlyCollection<OffroadTripMember> Members => _members;

    private readonly List<OffroadTripHistory> _histories = new();
    public IReadOnlyCollection<OffroadTripHistory> History => _histories;

    protected OffroadTrip() { }

    private OffroadTrip(string title, string description, DateTime startDate, DateTime endDate, List<string> tags,
        int maxParticipants, double price)
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

    public OffroadRoute AddRoute(int startDay, int endDay, string name, string note, string trackGeoJson,
        RouteSource source, double distanceMeters, double elevationGainMeters, string? originalGpx = null)
    {
        var route = new OffroadRoute(startDay, endDay, name, note, trackGeoJson, source, distanceMeters,
            elevationGainMeters, originalGpx);
        _routes.Add(route);
        return route;
    }

    public void RemoveRoute(int id)
    {
        var route = _routes.FirstOrDefault(r => r.Id == id);
        if (route == null) throw new Exception("Route not found");
        _routes.Remove(route);
    }

    public void UpdateRoute(int id, int startDay, int endDay, string name, string note, string trackGeoJson,
        RouteSource source, double distanceMeters, double elevationGainMeters, string? originalGpx = null)
    {
        var route = _routes.FirstOrDefault(r => r.Id == id);
        if (route == null) throw new Exception("Route not found");
        route.Update(startDay, endDay, name, note, trackGeoJson, source, distanceMeters, elevationGainMeters,
            originalGpx);
    }

    public void RequestMember(int userId)
    {
        if (_members.Any(m => m.UserId == userId)) throw new Exception("User already in trip");
        _members.Add(new OffroadTripMember(userId, Types.Request, Roles.Member, MemberStatus.Requested));
    }

    public void InivteMember(int userId)
    {
        if (_members.Any(m => m.UserId == userId)) throw new Exception("User already in trip");
        _members.Add(new OffroadTripMember(userId, Types.Invite, Roles.Member, MemberStatus.Invited));
    }

    public void DeclineMember(int userId)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member == null) throw new Exception("User not in trip");
        _members.Remove(member);
    }

    public void AcceptMember(int userId)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member == null) throw new Exception("User not in trip");
        member.Accept();
        member.UpdateRole(Roles.Member);
    }

    public void AddOwner(int userId)
    {
        _members.Add(new OffroadTripMember(userId, Types.Request, Roles.Owner, MemberStatus.Accepted));
    }

    public void RemoveMember(int userId)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member == null) throw new Exception("User not in trip");
        _members.Remove(member);
    }

    public void UpdateMemberRole(int userId, Roles role)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member == null) throw new Exception("User not in trip");
        member.UpdateRole(role);
    }

    public void AddHistory(string content) => _histories.Add(new OffroadTripHistory(DateTime.UtcNow, content));

    public void SetImageUrl(string imageUrl) => ImageUrl = imageUrl;

    public void UpdateTrip(string title, string description, DateTime startDate, DateTime endDate, string status,
        List<string> tags, int maxParticipants, double price)
    {
        Title = title;
        Description = description;
        StartDate = startDate;
        EndDate = endDate;
        if (status.Equals("Upcoming")) Status = Status.Upcoming;
        else if (status.Equals("Started")) Status = Status.Started;
        else if (status.Equals("Finished")) Status = Status.Finished;
        Tags = tags;
        MaxParticipants = maxParticipants;
        Price = price;
    }

    public void UpdateEmbedding(Vector embedding) => Embedding = embedding;

    public static OffroadTrip Create(string title, string description, DateTime startDate, DateTime endDate,
        List<string> tags, int maxParticipants, double price, int userId)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new Exception("Title can not be empty");
        if (startDate > endDate) throw new Exception("Invalid Date");
        if (startDate < DateTime.UtcNow) throw new Exception("Trip must be in future");
        var trip = new OffroadTrip(title, description, startDate, endDate, tags, maxParticipants, price);
        trip.AddOwner(userId);
        return trip;
    }
}
