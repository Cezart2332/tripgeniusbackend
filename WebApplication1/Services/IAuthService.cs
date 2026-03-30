using WebApplication1.DTOs.User;

namespace WebApplication1.Services;

public interface IAuthService
{
    public Task<UserResponse> Register(UserRequest userRequest);
}