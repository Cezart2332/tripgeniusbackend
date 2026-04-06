
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
}