using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TripGeniusBackend.Application.Interfaces;
using TripGeniusBackend.Application.Settings;
using WebPush;

public class NotificationService : INotificationService
{
    private readonly WebPushClient _client;
    private readonly VapidDetails _vapid;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(WebPushClient client, IOptions<VapidSettings> vapidSettings, IServiceScopeFactory scopeFactory, ILogger<NotificationService> logger)
    {
        _client = client;
        _vapid = new VapidDetails(vapidSettings.Value.Subject, vapidSettings.Value.PublicKey, vapidSettings.Value.PrivateKey);
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task SendNotificationAsync(int userId, string title, string body, string url = "/app")
    {
        using var scope = _scopeFactory.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var subscriptions = await userRepository.GetUserSubscriptions(userId);
        
        if (!subscriptions.Any())
        {
            _logger.LogWarning("No subscriptions found for user {UserId}", userId);
            return;
        }

        var payload = JsonSerializer.Serialize(new { title, body, url });

        foreach (var sub in subscriptions)
        {
            try
            {
                var pushSub = new WebPush.PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);
                await _client.SendNotificationAsync(pushSub, payload, _vapid);
                _logger.LogInformation("Push sent to user {UserId} → {Endpoint}", userId, sub.Endpoint);
            }
            catch (WebPushException ex) when (ex.StatusCode == HttpStatusCode.Gone)
            {
                _logger.LogWarning("Subscription expired for user {UserId}, removing", userId);
                userRepository.DeleteSubscription(sub);
            }
            catch (WebPushException ex)
            {
                _logger.LogError("Push failed for user {UserId}: {Status} {Message}", 
                    userId, ex.StatusCode, ex.Message);
            }
        }

        await userRepository.SaveChanges();
    }
}