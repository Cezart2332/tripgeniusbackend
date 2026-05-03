using System.Net;
using Microsoft.Extensions.Options;
using System.Text.Json;
using TripGeniusBackend.Application.Interfaces;
using TripGeniusBackend.Application.Settings;
using WebPush;

namespace TripGeniusBackend.Infrastructure.Persistence.Services;

public class NotificationService : INotificationService
{
    private readonly WebPushClient _client;
    private readonly VapidDetails _vapid;
    private readonly IUserRepository _userRepository;

    public NotificationService(WebPushClient client, IOptions<VapidSettings> vapidSettings, IUserRepository userRepository)
    {
        _client = client;
        _vapid = new VapidDetails(vapidSettings.Value.Subject, vapidSettings.Value.PublicKey, vapidSettings.Value.PrivateKey);
        _userRepository = userRepository;
    }

    public async Task SendNotificationAsync(int userId, string title, string body, string url = "/app")
    {
        var subscriptions = await _userRepository.GetUserSubscriptions(userId);
        var payload = JsonSerializer.Serialize(new { title, body, url });

        foreach (var sub in subscriptions)
        {
            try
            {
                var pushSub = new WebPush.PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);
                await _client.SendNotificationAsync(pushSub, payload, _vapid);
            }
            catch (WebPushException ex) when (ex.StatusCode == HttpStatusCode.Gone)
            {
                _userRepository.DeleteSubscription(sub);
            }
        }

        await _userRepository.SaveChanges();
    }
}