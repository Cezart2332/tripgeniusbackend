using TripGeniusBackend.Application.DTOs.User;
using TripGeniusBackend.Application.Interfaces;
using TripGeniusBackend.Application.Interfaces.Queries;
using TripGeniusBackend.Application.Interfaces.UseCases;
using TripGeniusBackend.Domain.Entities;
using TripGeniusBackend.Domain.Enums;

namespace TripGeniusBackend.Application.UseCases;

public class BugService : IBugService
{
    private readonly IBugRepository _bugRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUserQueryService _userQueryService;
    private readonly IJwtService _jwtService;

    public BugService(IBugRepository bugRepository, IJwtService jwtService, IUserRepository userRepository, IUserQueryService userQueryService)
    {
        _bugRepository = bugRepository;
        _jwtService = jwtService;
        _userRepository = userRepository;
        _userQueryService = userQueryService;
    }

    public async Task ReportBug(BugRequest bugRequest)
    {
        if (bugRequest == null) throw new ArgumentNullException(nameof(bugRequest));
        if (string.IsNullOrEmpty(bugRequest.Description)) throw new ArgumentException("Description cannot be empty", nameof(bugRequest));

        var user = await _userRepository.GetUserById(_jwtService.GetUserId());
        if(user == null) throw new KeyNotFoundException("User not found");
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