using WebApplication1.Data;
using WebApplication1.DTOs.User;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

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

        var user = await _context.Users.Include(u => u.Profile).Include(u => u.Preferences).FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) throw new Exception("User not found");

        return new UserResponse
        {
            Username = user.Profile.Username,
            Email = user.Email,
            ProfileUrl = user.Profile.ProfileURL,
            Description = user.Profile.Description,
            Tags = user.Preferences.Tags,
            GroupSize = user.Preferences.MaxGroupSize
        };
    }

    public async Task<UserResponse> Update(UpdateRequest updateRequest)
    {
        var userId = int.Parse(_httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier));

        var user = await _context.Users.Include(u => u.Profile).Include(u => u.Preferences).FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) throw new Exception("User not found");
        if (updateRequest.Avatar != null)
        {
            var file = updateRequest.Avatar;
            var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "avatars");
            Directory.CreateDirectory(uploadFolder);

            var filename = $"{Guid.NewGuid()}.{Path.GetExtension(file.FileName)}";
            var path = Path.Combine(uploadFolder, filename);
            using (var stream = new FileStream(path, FileMode.Create)) 
                await file.CopyToAsync(stream); 
            var url = $"{_httpContextAccessor.HttpContext!.Request.Scheme}://{_httpContextAccessor.HttpContext!.Request.Host}/avatars/{filename}"; 
            user.Profile.ProfileURL = url;

        }
        if (updateRequest.Description != null) user.Profile.Description = updateRequest.Description;
        if (updateRequest.Tags != null) user.Preferences.Tags = updateRequest.Tags;
        if (updateRequest.GroupSize != null) user.Preferences.MaxGroupSize = updateRequest.GroupSize;

        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        return new UserResponse
        {
            Username = user.Profile.Username,
            Email = user.Email,
            ProfileUrl = user.Profile.ProfileURL,
            Description = user.Profile.Description,
            Tags = user.Preferences.Tags,
            GroupSize = user.Preferences.MaxGroupSize
        };
    }
}