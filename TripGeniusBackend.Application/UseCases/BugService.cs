using TripGeniusBackend.Application.DTOs.User;
using TripGeniusBackend.Application.Interfaces;
using TripGeniusBackend.Domain.Entities;
using TripGeniusBackend.Domain.Enums;

namespace TripGeniusBackend.Application.UseCases;

public class BugService : IBugService
{
    private readonly IBugRepository _bugRepository;
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;

    public BugService(IBugRepository bugRepository, IJwtService jwtService, IUserRepository userRepository)
    {
        _bugRepository = bugRepository;
        _jwtService = jwtService;
        _userRepository = userRepository;
    }

    public async Task ReportBug(BugRequest bugRequest)
    {
        var user = await _userRepository.GetUserById(_jwtService.GetUserId());
        if(user == null) throw new Exception("User not found");
        Bug bug = new Bug
        {
            Description = bugRequest.Description,
            UserId = user.Id,
            User = user,
            Status = BugStatus.New,
            
        };
        await _bugRepository.CreateBug(bug);
    }
}