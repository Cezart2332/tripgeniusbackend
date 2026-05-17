using TripGeniusBackend.Domain.Entities;
using TripGeniusBackend.Domain.Enums;

namespace TripGeniusBackend.Tests.Builders;

/// <summary>
/// Builder pattern for creating test Trip entities with default or custom values
/// </summary>
public class TripBuilder
{
    private int _id = 1;
    private string _title = "Test Trip";
    private string _description = "A test trip for unit tests";
    private DateTime _startingDate = DateTime.UtcNow.AddDays(7);
    private DateTime _endingDate = DateTime.UtcNow.AddDays(14);
    private int _userId = 1;
    private Status _status = Status.Upcoming;
    private double _price = 100;
    private int _maxParticipants = 10;
    private List<string> _tags = new() { "adventure", "test" };

    public TripBuilder WithId(int id)
    {
        _id = id;
        return this;
    }

    public TripBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public TripBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public TripBuilder WithStartingDate(DateTime date)
    {
        _startingDate = date;
        return this;
    }

    public TripBuilder WithEndingDate(DateTime date)
    {
        _endingDate = date;
        return this;
    }

    public TripBuilder WithUserId(int userId)
    {
        _userId = userId;
        return this;
    }

    public TripBuilder WithStatus(Status status)
    {
        _status = status;
        return this;
    }

    public TripBuilder WithPrice(double price)
    {
        _price = price;
        return this;
    }

    public TripBuilder WithMaxParticipants(int max)
    {
        _maxParticipants = max;
        return this;
    }

    public TripBuilder WithTags(List<string> tags)
    {
        _tags = tags;
        return this;
    }

    public Trip Build()
    {
        return Trip.Create(_title, _description, _startingDate, _endingDate, _tags, _maxParticipants, _price, _userId);
    }
}
