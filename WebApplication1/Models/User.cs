using System.ComponentModel.DataAnnotations;
using BCrypt.Net;

namespace WebApplication1.Models;

public class User
{
    [Required]
    public int Id { get; set; }
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;
    
}