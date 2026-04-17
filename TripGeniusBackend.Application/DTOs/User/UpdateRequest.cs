namespace TripGeniusBackend.Application.DTOs.User;

public class UpdateRequest
{
    public Stream? AvatarStream { get; set; }
    public string? AvatarFileName { get; set; }
    
    public string? Username { get; set; }

    public string? Description { get; set; }
    public List<string>? Tags { get; set; }
    public int? GroupSize { get; set; }
    public double? Buget { get; set; }
}