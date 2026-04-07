using Microsoft.AspNetCore.Mvc;
using WebApplication1.DTOs.Auth;
using WebApplication1.Services;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }
    
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest registerRequest)
    {
        try
        {
            var result = await _authService.Register(registerRequest);
            return Ok(result);
        }
        catch (ArgumentException e)
        {
            return BadRequest(new { message = e.Message });
        }
    }
    
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
    {
        try
        {
            var result = await _authService.Login(loginRequest);
            return Ok(result);
        }
        catch (ArgumentException e)
        {
            return BadRequest(new { message = e.Message });
        }
    }
    
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        try
        {
            return Ok(await _authService.RefreshToken(refreshToken));
        }
        catch (ArgumentException e)
        {
            Console.WriteLine(e.Message);
            return BadRequest(new { message = e.Message });
        }
    }
    [HttpPost("logout")]
    public async Task Logout()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        await _authService.Logout(refreshToken);
    }

}