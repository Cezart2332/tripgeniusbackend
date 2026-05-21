using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TripGeniusBackend.Application.Helpers;
using TripGeniusBackend.Application.Interfaces.Services;
using TripGeniusBackend.Application.Settings;

namespace TripGeniusBackend.Infrastructure.Services;

public class ContentModerationService : IContentModerationService
{
    private readonly HttpClient _httpClient;
    private readonly ModerationSettings _settings;
    private readonly ILogger<ContentModerationService> _logger;

    public ContentModerationService(
        HttpClient httpClient,
        IOptions<ModerationSettings> settings,
        ILogger<ContentModerationService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<ModerationCheckResult> CheckTextAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled || !_settings.TextEnabled || string.IsNullOrWhiteSpace(text))
            return new ModerationCheckResult(false);

        if (ProfanityFilter.ContainsProfanity(text))
        {
            _logger.LogInformation("Text blocked by local profanity filter.");
            return new ModerationCheckResult(
                true,
                "Your message was flagged as inappropriate. Please revise and try again.");
        }

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "/text-check",
                new { text },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Text moderation returned {StatusCode}; {Policy}.",
                    (int)response.StatusCode,
                    _settings.FailOpen ? "allowing content (fail-open)" : "blocking content");
                return _settings.FailOpen
                    ? new ModerationCheckResult(false)
                    : new ModerationCheckResult(true, "Moderation service unavailable. Try again later.");
            }

            var result = await response.Content.ReadFromJsonAsync<TextCheckResponse>(cancellationToken);
            if (result is null)
                return new ModerationCheckResult(false);

            _logger.LogInformation(
                "Text moderation: decision={Decision} is_toxic={IsToxic} scores={@Scores}",
                result.Decision ?? (result.IsToxic ? "block" : "allow"),
                result.IsToxic,
                result.Scores);

            if (result.IsToxic)
            {
                return new ModerationCheckResult(
                    true,
                    "Your message was flagged as inappropriate. Please revise and try again.");
            }

            return new ModerationCheckResult(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            _logger.LogWarning(
                ex,
                "Text moderation unavailable; {Policy}.",
                _settings.FailOpen ? "allowing content (fail-open)" : "blocking content");
            return _settings.FailOpen
                ? new ModerationCheckResult(false)
                : new ModerationCheckResult(true, "Moderation service unavailable. Try again later.");
        }
    }

    public async Task<ModerationCheckResult> CheckImageAsync(
        Stream imageStream,
        string? contentType,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled || !_settings.ImageEnabled)
            return new ModerationCheckResult(false);

        try
        {
            var imageBytes = await ReadAllBytesAsync(imageStream, cancellationToken);

            using var content = new MultipartFormDataContent();
            var body = new ByteArrayContent(imageBytes);
            body.Headers.ContentType = new MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(contentType) ? "image/jpeg" : contentType);
            content.Add(body, "file", "upload.jpg");

            using var response = await _httpClient.PostAsync("/image-check", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Image moderation returned {StatusCode}; {Policy}. Body: {Body}",
                    (int)response.StatusCode,
                    _settings.FailOpen ? "allowing upload (fail-open)" : "blocking upload",
                    errorBody);
                return _settings.FailOpen
                    ? new ModerationCheckResult(false)
                    : new ModerationCheckResult(true, "Image moderation is unavailable. Try again later.");
            }

            var result = await response.Content.ReadFromJsonAsync<ImageCheckResponse>(cancellationToken);
            if (result is null)
                return new ModerationCheckResult(false);

            _logger.LogInformation(
                "Image moderation: decision={Decision} is_nsfw={IsNsfw} nsfw_score={Score} debug={@Debug}",
                result.Decision ?? (result.IsNsfw ? "block" : "allow"),
                result.IsNsfw,
                result.NsfwScore,
                result.Debug);

            if (result.IsNsfw)
            {
                return new ModerationCheckResult(
                    true,
                    "This image was flagged as inappropriate and cannot be uploaded.");
            }

            return new ModerationCheckResult(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            _logger.LogWarning(
                ex,
                "Image moderation unavailable; {Policy}. BaseUrl={BaseUrl}",
                _settings.FailOpen ? "allowing upload (fail-open)" : "blocking upload",
                _settings.BaseUrl);
            return _settings.FailOpen
                ? new ModerationCheckResult(false)
                : new ModerationCheckResult(true, "Image moderation is unavailable. Try again later.");
        }
        finally
        {
            if (imageStream.CanSeek)
                imageStream.Position = 0;
        }
    }

    private static async Task<byte[]> ReadAllBytesAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (stream is MemoryStream memory)
        {
            if (memory.CanSeek)
                memory.Position = 0;
            return memory.ToArray();
        }

        if (stream.CanSeek)
            stream.Position = 0;

        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();

        if (stream.CanSeek)
            stream.Position = 0;

        return bytes;
    }

    private sealed class ImageCheckResponse
    {
        [JsonPropertyName("is_nsfw")]
        public bool IsNsfw { get; set; }

        [JsonPropertyName("nsfw_score")]
        public double NsfwScore { get; set; }

        [JsonPropertyName("decision")]
        public string? Decision { get; set; }

        [JsonPropertyName("debug")]
        public Dictionary<string, object>? Debug { get; set; }
    }

    private sealed class TextCheckResponse
    {
        [JsonPropertyName("is_toxic")]
        public bool IsToxic { get; set; }

        [JsonPropertyName("scores")]
        public Dictionary<string, double>? Scores { get; set; }

        [JsonPropertyName("decision")]
        public string? Decision { get; set; }

        [JsonPropertyName("debug")]
        public Dictionary<string, object>? Debug { get; set; }
    }
}
