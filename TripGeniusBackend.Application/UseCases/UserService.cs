using TripGeniusBackend.Application.DTOs.User;
using TripGeniusBackend.Application.Helpers;
using TripGeniusBackend.Application.Interfaces;
using TripGeniusBackend.Application.Interfaces.Queries;
using TripGeniusBackend.Application.Interfaces.Services;
using TripGeniusBackend.Application.Interfaces.UseCases;
using TripGeniusBackend.Application.Moderation;
using TripGeniusBackend.Domain.Entities;

namespace TripGeniusBackend.Application.UseCases;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserQueryService _userQueryService;
    private readonly IJwtService _jwtService;
    private readonly IFileUploader _fileUploader;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IBackgroundModerationService _backgroundModeration;

    public UserService(
        IUserRepository userRepository,
        IUserQueryService userQueryService,
        IJwtService jwtService,
        IFileUploader fileUploader,
        IPasswordHasher passwordHasher,
        IBackgroundModerationService backgroundModeration)
    {
        _userRepository = userRepository;
        _userQueryService = userQueryService;
        _jwtService = jwtService;
        _fileUploader = fileUploader;
        _passwordHasher = passwordHasher;
        _backgroundModeration = backgroundModeration;
    }
    
    public async Task<UserResponse?> GetMe()
    {
        var user = await _userQueryService.GetUserDetails(_jwtService.GetUserId());
        return user;
    }

    public async Task<UserResponse> Update(UpdateRequest updateRequest)
    {
        var userId = _jwtService.GetUserId();
        var user = await _userRepository.GetUserById(userId);
        if (user == null) throw new KeyNotFoundException("User not found");

        var avatarBytes = await StreamBuffer.ReadAllBytesAsync(updateRequest.AvatarStream);

        string? url = null;
        if (updateRequest.AvatarStream != null)
        { 
            url = await _fileUploader.UploadFile(updateRequest.AvatarStream,Path.GetExtension(updateRequest.AvatarFileName),"avatar", user.Id);
        }
        user.UpdateProfile(updateRequest.Username ?? user.Profile.Username, url ?? user.Profile.ProfileURL, updateRequest.Description ?? user.Profile.Description);
        
        user.UpdatePreferences(updateRequest.GroupSize ?? user.Preferences.MaxGroupSize, updateRequest.Tags ?? user.Preferences.Tags);
        
        await _userRepository.SaveChanges();

        _backgroundModeration.ScheduleTextReview(
            ModerationTarget.UserProfile,
            userId,
            userId,
            ModerationFields.ToReviewList(ModerationFields.FromProfileUpdate(
                updateRequest.Username ?? user.Profile.Username,
                updateRequest.Description ?? user.Profile.Description,
                updateRequest.Tags ?? user.Preferences.Tags)));
        if (avatarBytes is { Length: > 0 })
            _backgroundModeration.ScheduleImageReview(
                ModerationTarget.UserAvatar, userId, userId, avatarBytes, ImageContentType(updateRequest.AvatarFileName));
        
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

    private static string? ImageContentType(string? fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return null;
        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            _ => "image/jpeg",
        };
    }

    public async Task ChangeMail(ChangeEmailRequest changeEmailRequest)
    {
        var newEmail = changeEmailRequest.NewEmail;
        
        if(await _userRepository.UserExists(newEmail)) throw new ArgumentException("Email already exists");
        var user = await _userRepository.GetUserById(_jwtService.GetUserId());
        if(user == null) throw new KeyNotFoundException("User not found");
        user.UpdateEmail(newEmail);
        await _userRepository.SaveChanges();
    }

    public async Task ChangePassword(ChangePasswordRequest changePasswordRequest)
    {
        var oldPassword = changePasswordRequest.OldPassword; 
        var newPassword = changePasswordRequest.NewPassword;
        if(newPassword.Length < 8) throw new ArgumentException("Password must be at least 8 characters long");
        var user = await _userRepository.GetUserById(_jwtService.GetUserId());
        if(user == null) throw new KeyNotFoundException("User not found");
        if(!_passwordHasher.VerifyPassword(oldPassword,user.Password)) throw new ArgumentException("Invalid password");
        user.UpdatePassword(_passwordHasher.HashPassword(newPassword));
        await _userRepository.SaveChanges();
    }

    public async Task DeleteAccount()
    {
        var user = await _userRepository.GetUserById(_jwtService.GetUserId());
        if(user == null) throw new KeyNotFoundException("User not found");
        _fileUploader.DeleteFolder("avatar",user.Id);
        await _userRepository.DeleteUser(user);
        await _userRepository.SaveChanges();
    }
    public async Task<List<UserResponse>> SearchUsersByEmail(UsersRequest usersRequest)
    {
        var users = await _userQueryService.GetUserByUsername(usersRequest);
        return users;
    }

    public async Task ReadNotifications()
    {
        var user = await _userRepository.GetUserById(_jwtService.GetUserId());
        if(user == null) throw new KeyNotFoundException("User not found");
        user.ReadAllNotifications();
        await _userRepository.SaveChanges();
    }

    public async Task MarkNotificationAsRead(int id)
    {
        var user = await _userRepository.GetUserById(_jwtService.GetUserId());
        if(user == null) throw new KeyNotFoundException("User not found");

        user.ReadNotification(id);
        await _userRepository.SaveChanges();
    }

    public async Task SubscribeToNotifications(string endpoint, string auth, string p256dh)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new ArgumentException("Push endpoint is required.");
        if (string.IsNullOrWhiteSpace(p256dh) || string.IsNullOrWhiteSpace(auth))
            throw new ArgumentException("Push subscription keys are required.");

        int userId = _jwtService.GetUserId();
        var user = await _userRepository.GetUserById(userId);
        if (user == null) throw new KeyNotFoundException("User not found");

        var existing = user.PushSubscription;
        if (existing != null
            && existing.Endpoint == endpoint
            && existing.P256dh == p256dh
            && existing.Auth == auth)
            return;

        await _userRepository.DetachEndpointFromOtherUsersAsync(endpoint, userId);
        user.SubscribeToPush(endpoint, p256dh, auth);
        await _userRepository.SaveChanges();
    }
}