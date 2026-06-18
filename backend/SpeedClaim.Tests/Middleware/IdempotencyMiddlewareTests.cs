using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using SpeedClaim.Api.Filters;

namespace SpeedClaim.Tests.Middleware;

[TestFixture]
public class IdempotentAttributeTests
{
    private Mock<IDistributedCache> _mockCache = null!;
    private IdempotentAttribute _attribute = null!;

    [SetUp]
    public void Setup()
    {
        _mockCache = new Mock<IDistributedCache>();
        _attribute = new IdempotentAttribute();
    }

    private ActionExecutingContext CreateActionExecutingContext(string? idempotencyKey)
    {
        var httpContext = new DefaultHttpContext();
        if (idempotencyKey != null)
            httpContext.Request.Headers["Idempotency-Key"] = idempotencyKey;

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(_mockCache.Object);
        httpContext.RequestServices = serviceCollection.BuildServiceProvider();

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new ActionExecutingContext(
            actionContext,
            new System.Collections.Generic.List<IFilterMetadata>(),
            new System.Collections.Generic.Dictionary<string, object?>(),
            new object());
    }

    [Test]
    public async Task MissingHeader_PassesThroughNormally()
    {
        var context = CreateActionExecutingContext(null);
        var nextCalled = false;

        await _attribute.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(
                new ActionContext(context.HttpContext, context.RouteData, context.ActionDescriptor),
                new System.Collections.Generic.List<IFilterMetadata>(),
                new object()));
        });

        Assert.That(nextCalled, Is.True);
    }

    [Test]
    public async Task InvalidGuidHeader_ReturnsBadRequest()
    {
        var context = CreateActionExecutingContext("not-a-guid");
        var nextCalled = false;

        await _attribute.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(
                new ActionContext(context.HttpContext, context.RouteData, context.ActionDescriptor),
                new System.Collections.Generic.List<IFilterMetadata>(),
                new object()));
        });

        Assert.That(nextCalled, Is.False);
        Assert.That(context.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task FirstRequest_ProcessesAndCachesResponse()
    {
        var key = Guid.NewGuid();
        var context = CreateActionExecutingContext(key.ToString());

        _mockCache.Setup(c => c.GetAsync($"Idempotent_{key}", It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        var executedContext = new ActionExecutedContext(
            new ActionContext(context.HttpContext, context.RouteData, context.ActionDescriptor),
            new System.Collections.Generic.List<IFilterMetadata>(),
            new object())
        {
            Result = new ObjectResult(new { id = "123" }) { StatusCode = 200 }
        };

        await _attribute.OnActionExecutionAsync(context, () => Task.FromResult(executedContext));

        Assert.That(context.Result, Is.Null);
        _mockCache.Verify(c => c.SetAsync(
            $"Idempotent_{key}",
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task DuplicateRequest_ReturnsCachedResponse()
    {
        var key = Guid.NewGuid();
        var context = CreateActionExecutingContext(key.ToString());

        var cachedResponse = JsonSerializer.Serialize(new { StatusCode = 200, Value = new { id = "cached-123" } });
        var cachedBytes = Encoding.UTF8.GetBytes(cachedResponse);

        _mockCache.Setup(c => c.GetAsync($"Idempotent_{key}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedBytes);

        var nextCalled = false;
        await _attribute.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(
                new ActionContext(context.HttpContext, context.RouteData, context.ActionDescriptor),
                new System.Collections.Generic.List<IFilterMetadata>(),
                new object()));
        });

        Assert.That(nextCalled, Is.False);
        Assert.That(context.Result, Is.TypeOf<ObjectResult>());
        Assert.That(context.HttpContext.Response.Headers["X-Idempotent-Replay"].ToString(), Is.EqualTo("true"));
    }

    [Test]
    public async Task ErrorResponse_NotCached()
    {
        var key = Guid.NewGuid();
        var context = CreateActionExecutingContext(key.ToString());

        _mockCache.Setup(c => c.GetAsync($"Idempotent_{key}", It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        var executedContext = new ActionExecutedContext(
            new ActionContext(context.HttpContext, context.RouteData, context.ActionDescriptor),
            new System.Collections.Generic.List<IFilterMetadata>(),
            new object())
        {
            Result = new ObjectResult(new { error = "not found" }) { StatusCode = 404 }
        };

        await _attribute.OnActionExecutionAsync(context, () => Task.FromResult(executedContext));

        _mockCache.Verify(c => c.SetAsync(
            It.IsAny<string>(),
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task ServerErrorResponse_NotCached()
    {
        var key = Guid.NewGuid();
        var context = CreateActionExecutingContext(key.ToString());

        _mockCache.Setup(c => c.GetAsync($"Idempotent_{key}", It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        var executedContext = new ActionExecutedContext(
            new ActionContext(context.HttpContext, context.RouteData, context.ActionDescriptor),
            new System.Collections.Generic.List<IFilterMetadata>(),
            new object())
        {
            Result = new ObjectResult(new { error = "server error" }) { StatusCode = 500 }
        };

        await _attribute.OnActionExecutionAsync(context, () => Task.FromResult(executedContext));

        _mockCache.Verify(c => c.SetAsync(
            It.IsAny<string>(),
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task CustomCacheDuration_UsedInCacheOptions()
    {
        var customAttribute = new IdempotentAttribute(cacheTimeInMinutes: 120);
        var key = Guid.NewGuid();
        var context = CreateActionExecutingContext(key.ToString());

        _mockCache.Setup(c => c.GetAsync($"Idempotent_{key}", It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        var executedContext = new ActionExecutedContext(
            new ActionContext(context.HttpContext, context.RouteData, context.ActionDescriptor),
            new System.Collections.Generic.List<IFilterMetadata>(),
            new object())
        {
            Result = new ObjectResult(new { id = "456" }) { StatusCode = 201 }
        };

        await customAttribute.OnActionExecutionAsync(context, () => Task.FromResult(executedContext));

        _mockCache.Verify(c => c.SetAsync(
            $"Idempotent_{key}",
            It.IsAny<byte[]>(),
            It.Is<DistributedCacheEntryOptions>(o =>
                o.AbsoluteExpirationRelativeToNow == TimeSpan.FromMinutes(120)),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
