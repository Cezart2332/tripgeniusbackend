namespace TripGeniusBackend.Application.DTOs.Trip;

public class UpdateRoleRequest
{
    public int Id { get; set; }
    public int TripId { get; set; }
    public string Role { get; set; } = string.Empty;
}