using System.ComponentModel.DataAnnotations;

namespace TripGeniusBackend.Application.DTOs.Auth;

public class RegisterRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;
    [Required]
    public string Username { get; set; } = string.Empty;
    [Required]
    public List<String> Tags { get; set; }
    [Required]
    public int MaxGroupSize { get; set; }
    [Required]
    public double Buget { get; set; }
}