using System.ComponentModel.DataAnnotations;
using BCrypt.Net;

namespace WebApplication1.Models;

public class User
{
    [Required]
    public int Id { get; set; }
    [Required]
    public Profile Profile { get; set; }
    [Required]
    public Preferences Preferences { get; set; }
    
    [Required]
    [EmailAddress]
    public string Email { get; set; }
    [MinLength(8)]
    public string Password { get; set; }
    
}