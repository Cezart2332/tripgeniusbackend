using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripGeniusBackend.API.DTOs;
using TripGeniusBackend.API.Helpers;
using TripGeniusBackend.Application.DTOs.Notifications;
using TripGeniusBackend.Application.DTOs.User;
using TripGeniusBackend.Application.Interfaces;
using TripGeniusBackend.Application.Interfaces.Services;
using TripGeniusBackend.Application.Interfaces.UseCases;

namespace TripGeniusBackend.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IContentModerationService _moderation;

    public UserController(IUserService userService, IContentModerationService moderation)
    {
        _userService = userService;
        _moderation = moderation;
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        return Ok(await _userService.GetMe());
    }

    [Authorize]
    [HttpPut("update")]
    public async Task<IActionResult> Update([FromForm] InitialUpdateRequest initialUpdateRequest)
    {
        var (avatarStream, avatarRejection) = await ImageModeration.ValidateUploadAsync(
            initialUpdateRequest.Avatar, _moderation);
        if (avatarRejection != null)
            return avatarRejection;

        try
        {
            var updateRequest = new UpdateRequest
            {
                Username = initialUpdateRequest.Username,
                Description = initialUpdateRequest.Description,
                AvatarFileName = initialUpdateRequest.Avatar?.FileName,
                AvatarStream = avatarStream,
                Tags = initialUpdateRequest.Tags,
                GroupSize = initialUpdateRequest.GroupSize,
            };
            return Ok(await _userService.Update(updateRequest));
        }
        finally
        {
            if (avatarStream != null)
                await avatarStream.DisposeAsync();
        }
    }

    [Authorize]
    [HttpPatch("change-mail")]
    public async Task<IActionResult> ChangeMail([FromBody] ChangeEmailRequest changeEmailRequest)
    {
        try
        {
            await _userService.ChangeMail(changeEmailRequest);
            return Ok();
        }
        catch (ArgumentException e)
        {
            Console.WriteLine(e.Message);
            return BadRequest(new { message = e.Message });
        }

    }

    [Authorize]
    [HttpPatch("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest changePasswordRequest)
    {
        try
        {
            await _userService.ChangePassword(changePasswordRequest);
            return Ok();
        }
        catch (ArgumentException e)
        {
            Console.WriteLine(e.Message);
            return BadRequest(new { message = e.Message });
        }
    }

    [Authorize]
    [HttpDelete("delete-account")]
    public async Task<IActionResult> DeleteAccount()
    {
        await _userService.DeleteAccount();
        return Ok();
    }
    [Authorize]
    [HttpPost("search-users")]
    public async Task<IActionResult> SearchUsers([FromBody] UsersRequest usersRequest)
    {
        var users = await _userService.SearchUsersByEmail(usersRequest);
        return Ok(users);
    }

    [Authorize]
    [HttpPost("subscribe-to-notifications")]
    public async Task<IActionResult> SubscribeToNotifications([FromBody] PushSubscribe pushSubscribe)
    {
        await _userService.SubscribeToNotifications(pushSubscribe.Endpoint,pushSubscribe.Auth,pushSubscribe.P256dh);
        return Ok();
    }
    
    [Authorize]
    [HttpPost("read-notifications")]
    public async Task<IActionResult> ReadNotifications()
    {
        await _userService.ReadNotifications();
        return Ok();
    }

    [Authorize]
    [HttpPost("read-notification")]
    public async Task<IActionResult> ReadNotification(NotificationRequest notificationRequest)
    {
        await _userService.MarkNotificationAsRead(notificationRequest.NotificationId);
        return Ok();
    }
}