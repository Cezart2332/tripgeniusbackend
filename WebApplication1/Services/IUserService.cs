using WebApplication1.DTOs.User;

namespace WebApplication1.Services;



public interface IUserService
{
    public Task<UserResponse> GetMe();
}