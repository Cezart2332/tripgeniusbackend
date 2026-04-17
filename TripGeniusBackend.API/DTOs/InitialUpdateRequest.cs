namespace TripGeniusBackend.API.DTOs;

public class InitialUpdateRequest
{
    public IFormFile? Avatar { get; set; }
    
    public string? Username { get; set; }

    public string? Description { get; set; }
    public List<string>? Tags { get; set; }
    public int? GroupSize { get; set; }
    public double? Buget { get; set; }
}