using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace SpeedClaim.Api.Filters;

[AttributeUsage(AttributeTargets.Method)]
public sealed class IdempotentAttribute : Attribute, IAsyncActionFilter
{
    private const int DefaultCacheTimeInMinutes = 60;
    private readonly TimeSpan _cacheDuration;

    public IdempotentAttribute(int cacheTimeInMinutes = DefaultCacheTimeInMinutes)
    {
        _cacheDuration = TimeSpan.FromMinutes(cacheTimeInMinutes);
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(
                "Idempotency-Key",
                out StringValues idempotencyKeyValue) ||
            string.IsNullOrWhiteSpace(idempotencyKeyValue))
        {
            await next();
            return;
        }

        if (!Guid.TryParse(idempotencyKeyValue, out Guid idempotencyKey))
        {
            context.Result = new BadRequestObjectResult(
                "Invalid Idempotency-Key header — must be a valid UUID");
            return;
        }

        var cache = context.HttpContext.RequestServices
            .GetRequiredService<IDistributedCache>();

        string cacheKey = $"Idempotent_{idempotencyKey}";
        string? cachedResult = await cache.GetStringAsync(cacheKey);

        if (cachedResult is not null)
        {
            var response = JsonSerializer.Deserialize<IdempotentResponse>(cachedResult)!;
            var result = new ObjectResult(response.Value) { StatusCode = response.StatusCode };
            context.HttpContext.Response.Headers["X-Idempotent-Replay"] = "true";
            context.Result = result;
            return;
        }

        var executedContext = await next();

        if (executedContext.Result is ObjectResult { StatusCode: >= 200 and < 300 } objectResult)
        {
            int statusCode = objectResult.StatusCode ?? StatusCodes.Status200OK;
            var response = new IdempotentResponse(statusCode, objectResult.Value);

            await cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(response),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _cacheDuration
                });
        }
    }
}

internal sealed class IdempotentResponse
{
    public int StatusCode { get; }
    public object? Value { get; }

    public IdempotentResponse(int statusCode, object? value)
    {
        StatusCode = statusCode;
        Value = value;
    }
}
