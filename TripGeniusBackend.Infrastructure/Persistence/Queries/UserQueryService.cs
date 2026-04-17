using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TripGeniusBackend.Application.DTOs.User;
using TripGeniusBackend.Application.Interfaces.Queries;
using TripGeniusBackend.Domain.Entities;

namespace TripGeniusBackend.Infrastructure.Persistence.Queries;

public class UserQueryService : IUserQueryService
{
    private readonly AppDbContext _context;

    public UserQueryService(AppDbContext context)
    {
        _context = context;
    }
    

    
    public async Task<UserResponse?> GetUserDetails(int id)
    {
        return await _context.Users.Where(u => u.Id == id).Select(MapToUserResponse()).FirstOrDefaultAsync();
    }

    private static Expression<Func<User, UserResponse>> MapToUserResponse()
    {
        return user => new UserResponse
        {
            Id = user.Id,
            Username = user.Profile.Username,
            Email = user.Email,
            ProfileUrl = user.Profile.ProfileURL,
            Description = user.Profile.Description,
            Tags = user.Preferences.Tags,
            GroupSize = user.Preferences.MaxGroupSize
        };
    }
}