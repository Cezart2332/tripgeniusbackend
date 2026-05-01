using TripGeniusBackend.Application.DTOs.User;

namespace TripGeniusBackend.Application.Interfaces.UseCases;

public interface IUserService
{ 
        public Task<UserResponse?> GetMe();
    
        public Task<UserResponse> Update(UpdateRequest updateRequest);
    
        public Task ChangeMail(ChangeEmailRequest changeEmailRequest);
    
        public Task ChangePassword(ChangePasswordRequest changePasswordRequest);
        public Task DeleteAccount();
        public Task<List<UserResponse>> SearchUsersByEmail(UsersRequest usersRequest);
        
        public Task ReadNotifications();
        public Task MarkNotificationAsRead(int id);

}
