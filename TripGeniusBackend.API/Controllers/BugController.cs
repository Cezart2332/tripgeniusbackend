using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripGeniusBackend.Application.DTOs.User;
using TripGeniusBackend.Application.Interfaces;

namespace TripGeniusBackend.API.Controllers;


[ApiController]
[Route("api/[controller]")]
public class BugController : ControllerBase
{
    private readonly IBugService _bugService;

    public BugController(IBugService bugService)
    {
        _bugService = bugService;
    }
    
    [Authorize]
    [HttpPost("bug-report")]
    public async Task<IActionResult> ReportBug([FromBody] BugRequest bugRequest)
    {
        try
        {
            await _bugService.ReportBug(bugRequest);
            return Ok();
        }
        catch (ArgumentException e)
        {
            return BadRequest(new { message = e.Message });
        }
        
    }
}