namespace WebApplication1.DTOs.User;

public class UpdateRequest
{
    public IFormFile? Avatar { get; set; }
    public string? Description { get; set; }
    public List<string>? Tags { get; set; }
    public int? GroupSize { get; set; }
}