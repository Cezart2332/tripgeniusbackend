using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using TripGeniusBackend.Application.DTOs.Trip;
using TripGeniusBackend.Application.Interfaces;
using TripGeniusBackend.Application.Interfaces.Repositories;
using TripGeniusBackend.Domain.Entities;

namespace TripGeniusBackend.Infrastructure.Persistence.Hubs;

public class TripChatHub : Hub
{
    private readonly IUserRepository _userRepository;
    private readonly ITripRepository _tripRepository;
    private readonly IMessageRepository _messageRepository;

    public TripChatHub(IUserRepository userRepository, ITripRepository tripRepository, IMessageRepository messageRepository)
    {
        _userRepository = userRepository;
        _tripRepository = tripRepository;
        _messageRepository = messageRepository;
    }
    
    public async Task JoinTrip(int tripId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"trip-{tripId}");
    }

    public async Task LeaveTrip(int tripId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"trip-{tripId}");
    }

    public async Task SendMessage(int tripId, string content)
    {
        var userId = int.Parse(Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        var user = await _userRepository.GetUserById(userId);
        if(user == null) throw new KeyNotFoundException("User not found");
        
        var message = Message.Create(content,"",DateTime.UtcNow,userId,tripId);
        
        await _messageRepository.AddMessage(message);
        await _messageRepository.SaveChanges();
        
        var messageResponse = new MessageResponse
        {
            Id = message.Id,
            Content = message.Content,
            SentAt = message.Date,
            ImageUrl = message.ImageURL,
            Username = user.Profile.Username,
            ProfileUrl = user.Profile.ProfileURL

        };
        await Clients.Group($"trip-{tripId}").SendAsync("ReceiveMessage", messageResponse);
    }
}