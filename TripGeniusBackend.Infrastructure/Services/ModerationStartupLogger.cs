using System.Net.Http.Json;
using System.Text.Json.Serialization;
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

        var effectiveTimeout = ModerationHttpTimeouts.ClientSeconds(_settings.TimeoutSeconds);
        if (_settings.TimeoutSeconds != effectiveTimeout)
        {
            _logger.LogWarning(
                "Moderation TimeoutSeconds={Configured} is out of range; using {Effective}s (min {Min}, max {Max}).",
                _settings.TimeoutSeconds,
                effectiveTimeout,
                ModerationHttpTimeouts.MinClientSeconds,
                ModerationHttpTimeouts.MaxClientSeconds);
        }

        try
        {
            var client = _httpClientFactory.CreateClient(nameof(ModerationStartupLogger));
            client.Timeout = ModerationHttpTimeouts.StartupProbe;
            if (!string.IsNullOrWhiteSpace(_settings.BaseUrl))
                client.BaseAddress = new Uri(_settings.BaseUrl.TrimEnd('/') + "/");

            var healthUrl = new Uri(client.BaseAddress!, "health");
            using var response = await client.GetAsync(healthUrl, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var health = await response.Content.ReadFromJsonAsync<ModerationHealthPayload>(
                    cancellationToken);
                _logger.LogInformation(
                    "Moderation service reachable at {Url} (FailOpen={FailOpen}, Image={Image}, Text={Text}, "
                    + "image_ready={ImageReady}, text_ready={TextReady}, HttpTimeout={Timeout}s).",
                    _settings.BaseUrl,
                    _settings.FailOpen,
                    _settings.ImageEnabled,
                    _settings.TextEnabled,
                    health?.ImageReady,
                    health?.TextReady,
                    effectiveTimeout);

                if (_settings.TextEnabled && health?.TextReady == false)
                {
                    _logger.LogWarning(
                        "Moderation text model not ready (error={Error}). Probing /text-check warmup ...",
                        health?.TextLoadError);
                    using var warmup = await client.PostAsJsonAsync(
                        "text-check",
                        new { text = "warmup" },
                        cancellationToken);
                    if (!warmup.IsSuccessStatusCode)
                    {
                        _logger.LogError(
                            "Moderation text warmup failed: {Status}.",
                            (int)warmup.StatusCode);
                    }
                }
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

    private sealed class ModerationHealthPayload
    {
        [JsonPropertyName("image_ready")]
        public bool ImageReady { get; set; }

        [JsonPropertyName("text_ready")]
        public bool TextReady { get; set; }

        [JsonPropertyName("text_load_error")]
        public string? TextLoadError { get; set; }
    }
}
