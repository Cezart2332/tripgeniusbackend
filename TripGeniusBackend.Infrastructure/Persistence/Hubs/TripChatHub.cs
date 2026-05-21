using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using TripGeniusBackend.Application.DTOs.Trip;
using TripGeniusBackend.Application.Interfaces;
using TripGeniusBackend.Application.Interfaces.Repositories;
using TripGeniusBackend.Domain.Entities;

namespace TripGeniusBackend.Infrastructure.Persistence.Hubs;

public class TripChatHub : Hub
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<TripChatHub> _hubContext;

    public TripChatHub(IServiceScopeFactory scopeFactory, IHubContext<TripChatHub> hubContext)
    {
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
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
        using var scope = _scopeFactory.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var messageRepository = scope.ServiceProvider.GetRequiredService<IMessageRepository>();

        var userId = int.Parse(Context.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var user = await userRepository.GetUserById(userId);
        if (user == null)
            throw new KeyNotFoundException("User not found");

        var message = Message.Create(content, "", DateTime.UtcNow, userId, tripId);

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

        var groupName = $"trip-{tripId}";
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
                var repo = services.GetRequiredService<IMessageRepository>();
                var deleted = await repo.DeleteMessageAsync(messageId);
                if (deleted)
                    await repo.SaveChanges();
                return deleted;
            });
    }
}
