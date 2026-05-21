using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TripGeniusBackend.Application.Interfaces.Services;
using TripGeniusBackend.Application.Settings;

namespace TripGeniusBackend.Infrastructure.Persistence.Hubs;

internal static class ChatModerationRunner
{
    public static void Schedule<THub>(
        IServiceScopeFactory scopeFactory,
        IHubContext<THub> hubContext,
        string groupName,
        int messageId,
        int senderUserId,
        string content,
        Func<IServiceProvider, int, Task<bool>> tryDeleteMessageAsync)
        where THub : Hub
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var services = scope.ServiceProvider;
                var settings = services.GetRequiredService<IOptions<ModerationSettings>>().Value;
                if (!settings.Enabled || !settings.TextEnabled)
                    return;

                var moderation = services.GetRequiredService<IContentModerationService>();
                var result = await moderation.CheckTextAsync(content);
                if (!result.IsBlocked)
                    return;

                var deleted = await tryDeleteMessageAsync(services, messageId);
                if (!deleted)
                    return;

                await hubContext.Clients.Group(groupName).SendAsync("MessageRemoved", messageId);
                await hubContext.Clients.User(senderUserId.ToString()).SendAsync(
                    "MessageRejected",
                    new
                    {
                        messageId,
                        message = result.Reason
                            ?? "Your message was removed because it was flagged as inappropriate.",
                    });
            }
            catch (Exception ex)
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var logger = scope.ServiceProvider.GetService<ILoggerFactory>()?
                    .CreateLogger(nameof(ChatModerationRunner));
                logger?.LogWarning(ex, "Background chat moderation failed for message {MessageId}", messageId);
            }
        });
    }
}
