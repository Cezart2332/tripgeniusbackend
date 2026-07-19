using Microsoft.AspNetCore.Mvc;
using TripGeniusBackend.Application.DTOs.Auth;
using TripGeniusBackend.Application.Interfaces;
using TripGeniusBackend.Application.Interfaces.UseCases;

namespace TripGeniusBackend.API.Controllers;

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
        var result = await _authService.Login(loginRequest);
        return Ok(result);
    }

    [HttpPost("google-login")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLogin googleLogin)
    {
        var result = await _authService.LoginWithGoogle(googleLogin.IdToken);
        return Ok(result);
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
            return BadRequest(new { message = e.Message });
        }
    }

    [HttpPost("logout")]
    public async Task Logout()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        await _authService.Logout(refreshToken);
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail(VerifyRequest verifyRequest)
    {
        var authResponse = await _authService.VerifyEmail(verifyRequest.Token);
        return Ok(authResponse);
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> SendResetPassword(SendResetPasswordRequest sendResetPasswordRequest)
    {
        await _authService.SendResetPassword(sendResetPasswordRequest.Email);
        return Ok();
    }

    [HttpPatch("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest resetPasswordRequest)
    {
        var result = await _authService.ResetPassword(resetPasswordRequest.Token, resetPasswordRequest.Password);
        return Ok(result);
    }

}