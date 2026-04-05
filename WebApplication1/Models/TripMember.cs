using WebApplication1.Models.Enums;
namespace WebApplication1.Models;

public class TripMember
{
    public int Id { get; set; }
    public User User { get; set; }
    public Trip Trip { get; set; }
    public Roles Role { get; set; }
}