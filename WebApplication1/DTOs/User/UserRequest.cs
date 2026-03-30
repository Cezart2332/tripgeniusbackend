using System.ComponentModel.DataAnnotations;

namespace WebApplication1.DTOs.User;

public class UserRequest
{
    [Required]
    private string Name { get; set; }
    [Required]
    [EmailAddress]
    private string Email { get; set; }
    [MinLength(8)]
    private string Password { get; set; }
    
}