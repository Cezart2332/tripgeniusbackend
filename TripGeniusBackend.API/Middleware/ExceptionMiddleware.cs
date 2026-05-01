using TripGeniusBackend.Application.Exceptions;

namespace TripGeniusBackend.API.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
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
            AppException appEx          => (appEx.StatusCode, appEx.Message), 
            ArgumentException           => (400, ex.Message),
            UnauthorizedAccessException => (403, ex.Message),
            KeyNotFoundException        => (404, ex.Message),
            InvalidOperationException   => (409, ex.Message),
            _                           => (500, "A apărut o eroare internă")
        };

        if (statusCode == 500)
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(new
        {
            status = statusCode,
            message
        });
    }
}