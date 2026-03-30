using System.ComponentModel.DataAnnotations;

namespace WebApplication1.DTOs.User;

public class RegisterRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;
    
}