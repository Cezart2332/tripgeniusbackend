using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using TripGeniusBackend.Application.DTOs.Trip;
using TripGeniusBackend.Application.Interfaces;
using TripGeniusBackend.Application.Interfaces.Repositories;
using TripGeniusBackend.Application.Interfaces.Services;
using TripGeniusBackend.Domain.Entities;

namespace TripGeniusBackend.Infrastructure.Persistence.Hubs;

public class OffroadTripChatHub : Hub
{
    private readonly IServiceScopeFactory _scopeFactory;

    public OffroadTripChatHub(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public async Task JoinOffroadTrip(int tripId) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, $"offroad-{tripId}");

    public async Task LeaveOffroadTrip(int tripId) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"offroad-{tripId}");

    public async Task SendMessage(int tripId, string content)
    {
        using var scope = _scopeFactory.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var messageRepository = scope.ServiceProvider.GetRequiredService<IOffroadMessageRepository>();

        var userId = int.Parse(Context.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var user = await userRepository.GetUserById(userId);
        if (user == null) throw new KeyNotFoundException("User not found");

        var moderation = scope.ServiceProvider.GetRequiredService<IContentModerationService>();
        var moderationResult = await moderation.CheckTextAsync(content);
        if (moderationResult.IsBlocked)
            throw new HubException(moderationResult.Reason ?? "Message not allowed.");

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

        await Clients.Group($"offroad-{tripId}").SendAsync("ReceiveMessage", messageResponse);
    }
}
