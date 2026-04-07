using WebApplication1.DTOs.User;

namespace WebApplication1.Services;



public interface IUserService
{
    public Task<UserResponse> GetMe();
    
    public Task<UserResponse> Update(UpdateRequest updateRequest);
    
    public Task ChangeMail(ChangeEmailRequest changeEmailRequest);
    
    public Task ChangePassword(ChangePasswordRequest changePasswordRequest);
    
    public Task DeleteAccount();

    public Task ReportBug(BugRequest bugRequest);
}