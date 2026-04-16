namespace TripGeniusBackend.Domain.Entities;

public class Preferences
{
    public int Id { get; private set; }
    
    public int UserId { get; private set; }
    public User User { get; private set; }
    public int MaxGroupSize { get; private set; }
    public List<String> Tags { get; private set; }

    public void Update(int maxGroupSize, List<String> tags)
    {
        MaxGroupSize = maxGroupSize;
        Tags = tags;
    }
    
}