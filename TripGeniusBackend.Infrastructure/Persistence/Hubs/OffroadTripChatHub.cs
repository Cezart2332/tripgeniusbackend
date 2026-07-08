using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using TripGeniusBackend.Application.DTOs.Trip;
using TripGeniusBackend.Application.Interfaces.Repositories;
using TripGeniusBackend.Application.Interfaces;
using TripGeniusBackend.Domain.Entities;
using TripGeniusBackend.Domain.Enums;

namespace TripGeniusBackend.Infrastructure.Persistence.Hubs;

[Authorize]
public class OffroadTripChatHub : Hub
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<OffroadTripChatHub> _hubContext;

    public OffroadTripChatHub(IServiceScopeFactory scopeFactory, IHubContext<OffroadTripChatHub> hubContext)
    {
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
    }

    private int GetUserId()
    {
        var value = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(value, out var userId))
            throw new HubException("Unauthorized");
        return userId;
    }

    private static async Task EnsureMemberAsync(IOffroadTripRepository tripRepository, int tripId, int userId)
    {
        var trip = await tripRepository.GetTripById(tripId);
        if (trip == null)
            throw new HubException("Trip not found");
        if (!trip.Members.Any(m => m.UserId == userId && m.MemberStatus == MemberStatus.Accepted))
            throw new HubException("You are not a member of this trip");
    }

    public async Task JoinOffroadTrip(int tripId)
    {
        using var scope = _scopeFactory.CreateScope();
        var tripRepository = scope.ServiceProvider.GetRequiredService<IOffroadTripRepository>();
        await EnsureMemberAsync(tripRepository, tripId, GetUserId());
        await Groups.AddToGroupAsync(Context.ConnectionId, $"offroad-{tripId}");
    }

    public async Task LeaveOffroadTrip(int tripId) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"offroad-{tripId}");

    public async Task SendMessage(int tripId, string content)
    {
        using var scope = _scopeFactory.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var messageRepository = scope.ServiceProvider.GetRequiredService<IOffroadMessageRepository>();
        var tripRepository = scope.ServiceProvider.GetRequiredService<IOffroadTripRepository>();

        var userId = GetUserId();
        await EnsureMemberAsync(tripRepository, tripId, userId);
        var user = await userRepository.GetUserById(userId);
        if (user == null)
            throw new KeyNotFoundException("User not found");

        var message = OffroadMessage.Create(content, "", DateTime.UtcNow, userId, tripId);
        await messageRepository.AddMessage(message);
        await messageRepository.SaveChanges();

        var messageResponse = new MessageResponse
        {
            Id = message.Id,
            Content = message.Content,
            SentAt = message.Date,
            ImageUrl = message.ImageURL,
            Username = user.Profile.Username,
            ProfileUrl = user.Profile.ProfileURL
        };

        var groupName = $"offroad-{tripId}";
        await Clients.Group(groupName).SendAsync("ReceiveMessage", messageResponse);

        ChatModerationRunner.Schedule(
            _scopeFactory,
            _hubContext,
            groupName,
            message.Id,
            userId,
            content,
            async (services, messageId) =>
            {
                var repo = services.GetRequiredService<IOffroadMessageRepository>();
                var deleted = await repo.DeleteMessageAsync(messageId);
                if (deleted)
                    await repo.SaveChanges();
                return deleted;
            });
    }
}
