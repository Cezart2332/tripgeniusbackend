using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TripGeniusBackend.Application.Settings;

namespace TripGeniusBackend.Infrastructure.Services;

/// <summary>
/// Logs whether the moderation container is reachable at startup (common Coolify misconfiguration).
/// </summary>
public sealed class ModerationStartupLogger : IHostedService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ModerationSettings _settings;
    private readonly ILogger<ModerationStartupLogger> _logger;

    public ModerationStartupLogger(
        IHttpClientFactory httpClientFactory,
        IOptions<ModerationSettings> settings,
        ILogger<ModerationStartupLogger> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogWarning("Moderation is DISABLED (Moderation:Enabled=false).");
            return;
        }

        if (string.IsNullOrWhiteSpace(_settings.BaseUrl))
        {
            _logger.LogError("Moderation BaseUrl is empty — image/text HTTP checks will not run.");
            return;
        }

        try
        {
            var client = _httpClientFactory.CreateClient(nameof(ModerationStartupLogger));
            client.Timeout = TimeSpan.FromSeconds(Math.Clamp(_settings.TimeoutSeconds, 1, 30));
            var healthUrl = new Uri(new Uri(_settings.BaseUrl.TrimEnd('/') + "/"), "health");
            using var response = await client.GetAsync(healthUrl, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Moderation service reachable at {Url} (FailOpen={FailOpen}, Image={Image}, Text={Text}).",
                    _settings.BaseUrl,
                    _settings.FailOpen,
                    _settings.ImageEnabled,
                    _settings.TextEnabled);
            }
            else
            {
                _logger.LogError(
                    "Moderation health check failed: {Status} from {Url}. Uploads/chat may be blocked or fail-open.",
                    (int)response.StatusCode,
                    healthUrl);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Cannot reach moderation at {Url}. If FailOpen=true, NSFW uploads and ML text checks are BYPASSED.",
                _settings.BaseUrl);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
