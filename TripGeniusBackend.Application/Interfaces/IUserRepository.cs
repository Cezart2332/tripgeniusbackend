
using TripGeniusBackend.Domain.Entities;

namespace TripGeniusBackend.Application.Interfaces;

public interface IUserRepository
{
    public Task<bool> UserExists(string email);
    public Task<User?> GetUserById(int id);
    public Task<User?> GetUserDetailsById(int id);
    public Task<User?> GetUserByEmail(string email);
    public Task CreateUser(User user);
    public Task DeleteUser(User user);
    
    public Task SaveChanges();
}