using TripGeniusBackend.Application.DTOs.User;
using TripGeniusBackend.Domain.Entities;

namespace TripGeniusBackend.Application.Interfaces.Queries;

public interface IUserQueryService
{
    public Task<List<UserResponse>> GetUserByUsername(UsersRequest usersRequest);
    public Task<UserResponse?> GetUserDetails(int id);

}