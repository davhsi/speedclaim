using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SpeedClaim.Api.Exceptions;

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
            await HandleExceptionAsync(context, ex, _logger);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception, ILogger logger)
    {
        context.Response.ContentType = "application/problem+json";
        
        var statusCode = exception switch
        {
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            KeyNotFoundException => (int)HttpStatusCode.NotFound,
            ArgumentException => (int)HttpStatusCode.BadRequest,
            _ => (int)HttpStatusCode.InternalServerError
        };

        if (statusCode >= 500)
        {
            logger.LogError(exception, "A server error occurred.");
        }
        else
        {
            logger.LogWarning("Client error {StatusCode}: {Message}", statusCode, exception.Message);
        }

        context.Response.StatusCode = statusCode;

        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var isDevelopment = env == "Development";

        var result = JsonSerializer.Serialize(new
        {
            type = $"https://httpstatuses.com/{statusCode}",
            title = exception.GetType().Name,
            status = statusCode,
            detail = statusCode == 500 && !isDevelopment ? "An internal server error occurred." : exception.Message,
            traceId = context.TraceIdentifier
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        return context.Response.WriteAsync(result);
    }
}
