using Microsoft.AspNetCore.Cors.Infrastructure;
using TripGeniusBackend.Application.Exceptions;

namespace TripGeniusBackend.API.Middleware;

public class ExceptionMiddleware
{
    private const string CorsPolicyName = "frontend";
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly ICorsService _corsService;
    private readonly ICorsPolicyProvider _corsPolicyProvider;

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger,
        ICorsService corsService,
        ICorsPolicyProvider corsPolicyProvider)
    {
        _next = next;
        _logger = logger;
        _corsService = corsService;
        _corsPolicyProvider = corsPolicyProvider;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (statusCode, message) = ex switch
        {
            AppException appEx => (appEx.StatusCode, appEx.Message),
            ArgumentException => (400, ex.Message),
            UnauthorizedAccessException => (403, ex.Message),
            KeyNotFoundException => (404, ex.Message),
            InvalidOperationException => (409, ex.Message),
            _ => (500, "A apărut o eroare internă"),
        };

        if (statusCode == 500)
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);

        await ApplyCorsHeadersAsync(context);

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(new
        {
            status = statusCode,
            message,
        });
    }

    private async Task ApplyCorsHeadersAsync(HttpContext context)
    {
        var policy = await _corsPolicyProvider.GetPolicyAsync(context, CorsPolicyName);
        if (policy is null)
            return;

        var result = _corsService.EvaluatePolicy(context, policy);
        _corsService.ApplyResult(result, context.Response);
    }
}
