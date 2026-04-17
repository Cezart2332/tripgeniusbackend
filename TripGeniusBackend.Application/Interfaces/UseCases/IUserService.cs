using TripGeniusBackend.Application.DTOs.User;

namespace TripGeniusBackend.Application.Interfaces;

public interface IUserService
{ 
        public Task<UserResponse> GetMe();
    
        public Task<UserResponse> Update(UpdateRequest updateRequest);
    
        public Task ChangeMail(ChangeEmailRequest changeEmailRequest);
    
        public Task ChangePassword(ChangePasswordRequest changePasswordRequest);
        public Task DeleteAccount();

}
