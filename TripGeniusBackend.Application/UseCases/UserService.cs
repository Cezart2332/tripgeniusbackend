using TripGeniusBackend.Application.Interfaces;
using TripGeniusBackend.Application.DTOs.User;

namespace TripGeniusBackend.Application.UseCases;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly IFileUploader _fileUploader;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(IUserRepository userRepository, IJwtService jwtService, IFileUploader fileUploader, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _fileUploader = fileUploader;
        _passwordHasher = passwordHasher;
    }
    
    public async Task<UserResponse> GetMe()
    {
        var user = await _userRepository.GetUserDetailsById(_jwtService.GetUserId());
        if (user == null) throw new Exception("User not found");
        var profile = user.Profile;
        var preferences = user.Preferences;


        return new UserResponse
        {
            Username = profile.Username,
            Email = user.Email,
            ProfileUrl = profile.ProfileURL,
            Description = profile.Description,
            Tags = preferences.Tags,
            GroupSize = preferences.MaxGroupSize
        };
    }

    public async Task<UserResponse> Update(UpdateRequest updateRequest)
    {
        var user = await _userRepository.GetUserDetailsById(_jwtService.GetUserId());
        if (user == null) throw new Exception("User not found");
        string url = null;
        if (updateRequest.AvatarStream != null)
        { 
            url = await _fileUploader.UploadFile(updateRequest.AvatarStream,Path.GetExtension(updateRequest.AvatarFileName),"avatar", user.Id);

        }
        user.UpdateProfile(updateRequest.Username ?? user.Profile.Username, url ?? user.Profile.ProfileURL, updateRequest.Description ?? user.Profile.Description);
        
        user.UpdatePreferences(updateRequest.GroupSize ?? user.Preferences.MaxGroupSize, updateRequest.Tags ?? user.Preferences.Tags);
        
        await _userRepository.SaveChanges();
        
        return new UserResponse
        {
            Username = user.Profile.Username,
            Email = user.Email,
            ProfileUrl = user.Profile.ProfileURL,
            Description = user.Profile.Description,
            Tags = user.Preferences.Tags,
            GroupSize = user.Preferences.MaxGroupSize
        };
    }

    public async Task ChangeMail(ChangeEmailRequest changeEmailRequest)
    {
        var newEmail = changeEmailRequest.NewEmail;
        
        if(await _userRepository.UserExists(newEmail)) throw new ArgumentException("Email already exists");
        var user = await _userRepository.GetUserDetailsById(_jwtService.GetUserId());
        if(user == null) throw new Exception("User not found");
        user.UpdateEmail(newEmail);
        await _userRepository.SaveChanges();
    }

    public async Task ChangePassword(ChangePasswordRequest changePasswordRequest)
    {
        var oldPassword = changePasswordRequest.OldPassword; 
        var newPassword = changePasswordRequest.NewPassword;
        if(newPassword.Length < 8) throw new ArgumentException("Password must be at least 8 characters long");
        var user = await _userRepository.GetUserDetailsById(_jwtService.GetUserId());
        if(user == null) throw new Exception("User not found");
        if(!_passwordHasher.VerifyPassword(oldPassword,user.Password)) throw new ArgumentException("Invalid password");
        user.UpdatePassword(_passwordHasher.HashPassword(newPassword));
        await _userRepository.SaveChanges();
    }

    public async Task DeleteAccount()
    {
        var user = await _userRepository.GetUserDetailsById(_jwtService.GetUserId());
        if(user == null) throw new Exception("User not found");
        _fileUploader.DeleteFolder("avatar",user.Id);
        await _userRepository.DeleteUser(user);
        await _userRepository.SaveChanges();
    }


}