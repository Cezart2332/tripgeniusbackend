using Microsoft.EntityFrameworkCore;
using TripGeniusBackend.Application.Interfaces;
using TripGeniusBackend.Domain.Entities;

namespace TripGeniusBackend.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> UserExists(string email)
    {
        bool exists = await _context.Users.AnyAsync(u => u.Email.Equals(email));
        return exists;
    }

    public async Task<User?> GetUserById(int id)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
    }
    
    public async Task<User?> GetUserDetailsById(int id)
    {
        return await _context.Users.Include(u => u.Profile).Include(u => u.Preferences).FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetUserByEmail(string email)
    {
        User? user = _context.Users.FirstOrDefault(u => u.Email.Equals(email));
        if(user == null) throw new ArgumentException("User not found");
        return user;
    }

    public Task CreateUser(User user)
    {
        _context.Users.Add(user);
        return Task.CompletedTask;
    }
    public Task DeleteUser(User user)
    {
        _context.Users.Remove(user);
        return Task.CompletedTask;
    }
    public async Task SaveChanges()
    {
        await _context.SaveChangesAsync();
    }
    
}