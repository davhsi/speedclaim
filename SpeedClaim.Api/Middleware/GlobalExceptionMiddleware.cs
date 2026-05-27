using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SpeedClaim.Api.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
            _logger.LogError(ex, "An unhandled exception occurred processing the request: {Path}", context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        // Default to 500. We can map custom domain exceptions to 400/404 later.
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var response = new
        {
            Error = "Internal Server Error",
            Message = "An unexpected error occurred. Please try again later.",
            // In a real production app we might conditionally hide the details, 
            // but for this phase we return it to help with testing.
            Details = exception.Message 
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
