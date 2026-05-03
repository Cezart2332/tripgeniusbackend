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
    public async Task<User?> GetUserByEmail(string email)
    {
        User? user = await _context.Users.Include(u => u.Profile).Include(u => u.Preferences).Include(u => u.Notifications).FirstOrDefaultAsync(u => u.Email.Equals(email));
        return user;
    }

    public async Task<User?> GetUserByToken(string token)
    {
        User? user = await _context.Users.Include(u => u.Profile).Include(u => u.Preferences).Include(u => u.Notifications).FirstOrDefaultAsync(u => u.VerifyToken.Equals(token));
        return user;
    }

    public async Task<User?> GetUserByResetToken(string token)
    {
        User? user = await _context.Users.Include(u => u.Profile).Include(u => u.Preferences).Include(u => u.Notifications).FirstOrDefaultAsync(u => u.ResetToken.Equals(token));
        return user;
    }
    public async Task<User?> GetUserById(int id)
    {
        return await _context.Users.Include(u => u.Profile).Include(u => u.Preferences).Include(u => u.Notifications)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<List<PushSubscription>> GetUserSubscriptions(int userId)
    {
        return await _context.PushSubscriptions.Where(s => s.UserId == userId).ToListAsync();
    }

    public Task DeleteSubscription(PushSubscription subscription)
    {
        _context.PushSubscriptions.Remove(subscription);
        return Task.CompletedTask;
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