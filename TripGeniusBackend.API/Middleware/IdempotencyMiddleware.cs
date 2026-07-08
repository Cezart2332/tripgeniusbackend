using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;

namespace TripGeniusBackend.API.Middleware;

/// <summary>
/// De-duplicates replayed mutations. When a POST/PUT/PATCH/DELETE carries an
/// <c>Idempotency-Key</c> header, the first execution is processed normally and its
/// response is cached (scoped per user, or per IP when anonymous). Subsequent requests
/// with the same key replay the cached response without re-executing the handler.
/// A concurrent request with an in-flight key gets 409.
/// </summary>
public class IdempotencyMiddleware
{
    private const string HeaderName = "Idempotency-Key";
    private const string ReplayedHeader = "Idempotent-Replayed";
    private const long MaxCacheableBodyBytes = 1024 * 1024; // 1 MB
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(24);

    // Marks keys whose request is currently executing (atomic claim via TryAdd).
    private static readonly ConcurrentDictionary<string, byte> InFlight = new();

    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;

    public IdempotencyMiddleware(RequestDelegate next, IMemoryCache cache)
    {
        _next = next;
        _cache = cache;
    }

    private static bool IsMutating(string method) =>
        HttpMethods.IsPost(method) || HttpMethods.IsPut(method) ||
        HttpMethods.IsPatch(method) || HttpMethods.IsDelete(method);

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsMutating(context.Request.Method) ||
            !context.Request.Headers.TryGetValue(HeaderName, out var headerValues))
        {
            await _next(context);
            return;
        }

        var idempotencyKey = headerValues.ToString();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            await _next(context);
            return;
        }

        var scope = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? context.Connection.RemoteIpAddress?.ToString()
                    ?? "anonymous";
        var cacheKey = $"idem:{scope}:{idempotencyKey}";

        if (_cache.TryGetValue(cacheKey, out CachedResponse? cached) && cached is not null)
        {
            await WriteResponseAsync(context, cached);
            return;
        }

        if (!InFlight.TryAdd(cacheKey, 0))
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            await context.Response.WriteAsJsonAsync(new
            {
                status = 409,
                message = "A request with this Idempotency-Key is already being processed."
            });
            return;
        }

        var originalBody = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;
        try
        {
            await _next(context);

            context.Response.Body = originalBody;
            buffer.Position = 0;
            await buffer.CopyToAsync(originalBody);

            // Cache only final outcomes (2xx/4xx). 5xx is treated as transient so the
            // client can safely retry the same key.
            var status = context.Response.StatusCode;
            if (status is >= 200 and < 500 && buffer.Length <= MaxCacheableBodyBytes)
            {
                _cache.Set(cacheKey, new CachedResponse
                {
                    StatusCode = status,
                    ContentType = context.Response.ContentType,
                    Body = buffer.ToArray()
                }, Ttl);
            }
        }
        finally
        {
            context.Response.Body = originalBody;
            InFlight.TryRemove(cacheKey, out _);
        }
    }

    private static async Task WriteResponseAsync(HttpContext context, CachedResponse cached)
    {
        context.Response.StatusCode = cached.StatusCode;
        if (!string.IsNullOrEmpty(cached.ContentType))
            context.Response.ContentType = cached.ContentType;
        context.Response.Headers[ReplayedHeader] = "true";
        await context.Response.Body.WriteAsync(cached.Body);
    }

    private sealed class CachedResponse
    {
        public int StatusCode { get; init; }
        public string? ContentType { get; init; }
        public byte[] Body { get; init; } = Array.Empty<byte>();
    }
}
