using WebApplication1.Data;
using WebApplication1.DTOs.User;
using WebApplication1.Models;
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

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.User.Id == userId);
        var preferences = await _context.Preferences.FirstOrDefaultAsync(p => p.User.Id == userId);
        if (user == null) throw new Exception("User not found");

        return new UserResponse
        {
            Username = profile.Username,
            Email = user.Email,
            ProfileUrl = profile.ProfileURL,
            Description = profile.Description,
            Tags = preferences.Tags,
            GroupSize = preferences.MaxGroupSize
        };
    }

    public async Task<UserResponse> Update(UpdateRequest updateRequest)
    {
        var userId = int.Parse(_httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier));

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.User.Id == userId);
        var preferences = await _context.Preferences.FirstOrDefaultAsync(p => p.User.Id == userId);
        if (user == null) throw new Exception("User not found");
        if (updateRequest.Avatar != null)
        {
            var file = updateRequest.Avatar;
            var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "avatars", $"{userId}");
            if (Directory.Exists(uploadFolder))
            {
                Directory.Delete(uploadFolder, true);    
            } 
            Directory.CreateDirectory(uploadFolder);
            
            var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/webp" };
            if (!allowedMimeTypes.Contains(file.ContentType))
                throw new Exception("Invalid file type");

            var filename = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var path = Path.Combine(uploadFolder, filename);
            using (var stream = new FileStream(path, FileMode.Create)) 
                await file.CopyToAsync(stream); 
            var url = $"{_httpContextAccessor.HttpContext!.Request.Scheme}://{_httpContextAccessor.HttpContext!.Request.Host}/avatars/{userId}/{filename}"; 
            profile.ProfileURL = url;

        }
        if (updateRequest.Description != null) profile.Description = updateRequest.Description;
        if (updateRequest.Tags != null) preferences.Tags = updateRequest.Tags;
        if (updateRequest.GroupSize.HasValue) preferences.MaxGroupSize = updateRequest.GroupSize.Value;

        _context.Users.Update(user);
        _context.Profiles.Update(profile);
        _context.Preferences.Update(preferences);
        await _context.SaveChangesAsync();
        return new UserResponse
        {
            Username = profile.Username,
            Email = user.Email,
            ProfileUrl = profile.ProfileURL,
            Description = profile.Description,
            Tags = preferences.Tags,
            GroupSize = preferences.MaxGroupSize
        };
    }

    public async Task ChangeMail(ChangeEmailRequest changeEmailRequest)
    {
        Console.WriteLine($"Received {changeEmailRequest.NewEmail}");
        var newMail = changeEmailRequest.NewEmail;
        if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(newMail)) throw new ArgumentException("Invalid email address");
        var userId = int.Parse(_httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier));
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if(user == null) throw new ArgumentException("User not found");
        var exists = await _context.Users.AnyAsync(u => u.Email == newMail);
        if(exists) throw new ArgumentException("Email already exists");
        user.Email = newMail;
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task ChangePassword(ChangePasswordRequest changePasswordRequest)
    {
        var oldPassword = changePasswordRequest.OldPassword; 
        var newPassword = changePasswordRequest.NewPassword;
        if(newPassword.Length < 8) throw new ArgumentException("Password must be at least 8 characters long");
        var userId = int.Parse(_httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier));
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if(user == null) throw new ArgumentException("User not found");
        if(!BCrypt.Net.BCrypt.EnhancedVerify(oldPassword, user.Password)) throw new ArgumentException("Invalid password");
        user.Password = BCrypt.Net.BCrypt.EnhancedHashPassword(newPassword);
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAccount()
    {
        var userId = int.Parse(_httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier));
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if(user == null) throw new Exception("User not found");
        var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "avatars", $"{userId}");
        if (Directory.Exists(uploadFolder))
        {
            Directory.Delete(uploadFolder, true);    
        } 
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
    }

    public async Task ReportBug(BugRequest bugRequest)
    {
        var userId = int.Parse(_httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier));
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if(bugRequest == null || user == null || bugRequest.Description == null) throw new ArgumentException("Invalid request");
        Bug bug = new Bug
        {
            UserId = user.Id,
            User = user,
            Description = bugRequest.Description,
        };   
        await _context.Bugs.AddAsync(bug);
        await _context.SaveChangesAsync();
    }
}