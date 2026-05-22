
using TripGeniusBackend.Domain.Entities;

namespace TripGeniusBackend.Application.Interfaces;

public interface IUserRepository
{
    public Task<bool> UserExists(string email);
    public Task<User?> GetUserByEmail(string email);
    public Task<User?> GetUserById(int id);
    public Task<User?> GetUserByToken(string token);
    public Task<User?> GetUserByResetToken(string token);
    public Task<List<PushSubscription>> GetUserSubscriptions(int userId);
    public Task DeleteSubscription(PushSubscription subscription);
    public Task DetachEndpointFromOtherUsersAsync(string endpoint, int userId);
    public Task CreateUser(User user);
    public Task DeleteUser(User user);

    
    public Task SaveChanges();
}