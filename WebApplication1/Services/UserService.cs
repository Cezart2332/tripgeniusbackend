using WebApplication1.Data;
using WebApplication1.DTOs.User;
using System.Security.Claims;

namespace WebApplication1.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserService(AppDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
       _httpContextAccessor = httpContextAccessor; 
    }

    public async Task<UserResponse> GetMe()
    {
        var userId = int.Parse(_httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier));

        var user = await _context.Users.FindAsync(userId);

        return new UserResponse
        {
            Email = user.Email, Name = user.Name
        };
    }


}