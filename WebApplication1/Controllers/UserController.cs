
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.DTOs.User;
using WebApplication1.Services;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        return Ok(await _userService.GetMe());
    }

    [Authorize]
    [HttpPut("update")]
    public async Task<IActionResult> Update([FromForm] UpdateRequest updateRequest)
    {
        return Ok(await _userService.Update(updateRequest));
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
    [HttpPost("bug-report")]
    public async Task<IActionResult> ReportBug([FromBody] BugRequest bugRequest)
    {
        await _userService.ReportBug(bugRequest);
        return Ok();
    }

}
