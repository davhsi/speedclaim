using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using SpeedClaim.Api.Configuration;
using SpeedClaim.Api.Dtos.Catalog;
using SpeedClaim.Api.Exceptions;
using SpeedClaim.Api.Services;

namespace SpeedClaim.Tests.Services;

[TestFixture]
public class FastApiBrochureIngestionClientTests
{
    private const string ApiKey = "test-only-internal-key-that-is-long-enough";

    [Test]
    public async Task IngestAsync_Success_SendsTypedRequestAndInternalAuthentication()
    {
        string? capturedKey = null;
        string? capturedCorrelationId = null;
        string? capturedBody = null;
        var request = Request();
        var documentId = Guid.NewGuid();
        var handler = new StubHandler(async (message, _) =>
        {
            capturedKey = message.Headers.GetValues("X-Internal-Api-Key").Single();
            capturedCorrelationId = message.Headers.GetValues("X-Correlation-ID").Single();
            capturedBody = await message.Content!.ReadAsStringAsync();
            return JsonResponse(HttpStatusCode.OK, $$"""
                {
                  "requestId": "{{request.RequestId}}",
                  "brochureId": "{{request.BrochureId}}",
                  "documentId": "{{documentId}}",
                  "status": "Succeeded",
                  "pageCount": 12,
                  "parentChunkCount": 8,
                  "childChunkCount": 22,
                  "embeddingProvider": "FastEmbed",
                  "embeddingModel": "BAAI/bge-small-en-v1.5",
                  "embeddingDimension": 384
                }
                """);
        });
        var client = CreateClient(handler);

        var result = await client.IngestAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo("Succeeded"));
            Assert.That(result.PageCount, Is.EqualTo(12));
            Assert.That(result.BrochureId, Is.EqualTo(request.BrochureId));
            Assert.That(result.DocumentId, Is.EqualTo(documentId));
            Assert.That(capturedKey, Is.EqualTo(ApiKey));
            Assert.That(capturedCorrelationId, Is.EqualTo(request.RequestId.ToString()));
            Assert.That(capturedBody, Does.Contain($"\"brochureId\":\"{request.BrochureId}\""));
        });
    }

    [Test]
    public void IngestAsync_FastApiValidationFailure_UsesSanitizedProviderCode()
    {
        var handler = new StubHandler((_, _) => Task.FromResult(JsonResponse(
            HttpStatusCode.UnprocessableEntity,
            """{"error":{"code":"image_only_pdf","message":"not retained","requestId":"safe"}}""")));
        var client = CreateClient(handler);

        var exception = Assert.ThrowsAsync<BrochureIngestionException>(() => client.IngestAsync(Request()));

        Assert.That(exception!.ErrorCode, Is.EqualTo("image_only_pdf"));
        Assert.That(exception.Message, Does.Not.Contain("not retained"));
    }

    [Test]
    public void IngestAsync_MalformedProviderCode_FallsBackToSafeStatusCode()
    {
        var handler = new StubHandler((_, _) => Task.FromResult(JsonResponse(
            HttpStatusCode.ServiceUnavailable,
            """{"error":{"code":"secret value with spaces"}}""")));
        var client = CreateClient(handler);

        var exception = Assert.ThrowsAsync<BrochureIngestionException>(() => client.IngestAsync(Request()));

        Assert.That(exception!.ErrorCode, Is.EqualTo("ai_ingestion_unavailable"));
    }

    [Test]
    public void IngestAsync_NetworkFailure_IsMappedWithoutEndpointDetails()
    {
        var handler = new StubHandler((_, _) => throw new HttpRequestException("sensitive endpoint detail"));
        var client = CreateClient(handler);

        var exception = Assert.ThrowsAsync<BrochureIngestionException>(() => client.IngestAsync(Request()));

        Assert.That(exception!.ErrorCode, Is.EqualTo("ai_ingestion_unavailable"));
        Assert.That(exception.Message, Does.Not.Contain("sensitive"));
    }

    [Test]
    public void IngestAsync_InvalidConfiguration_FailsBeforeSending()
    {
        var handler = new StubHandler((_, _) => throw new AssertionException("HTTP should not be called"));
        var client = CreateClient(handler, options => options.InternalApiKey = "short");

        var exception = Assert.ThrowsAsync<BrochureIngestionException>(() => client.IngestAsync(Request()));

        Assert.That(exception!.ErrorCode, Is.EqualTo("ai_configuration_invalid"));
        Assert.That(handler.CallCount, Is.Zero);
    }

    [Test]
    public void IngestAsync_IncompleteSuccessResponse_IsRejected()
    {
        var request = Request();
        var handler = new StubHandler((_, _) => Task.FromResult(JsonResponse(HttpStatusCode.OK, $$"""
            {
              "requestId": "{{request.RequestId}}",
              "brochureId": "{{request.BrochureId}}",
              "documentId": "{{request.BrochureId}}",
              "status": "Succeeded",
              "pageCount": 0,
              "parentChunkCount": 0,
              "childChunkCount": 0,
              "embeddingProvider": "",
              "embeddingModel": "",
              "embeddingDimension": 0
            }
            """)));
        var client = CreateClient(handler);

        var exception = Assert.ThrowsAsync<BrochureIngestionException>(() => client.IngestAsync(request));

        Assert.That(exception!.ErrorCode, Is.EqualTo("invalid_ingestion_response"));
    }

    private static FastApiBrochureIngestionClient CreateClient(
        StubHandler handler,
        Action<AiServiceOptions>? configure = null)
    {
        var options = new AiServiceOptions
        {
            BaseUrl = "http://127.0.0.1:8000",
            InternalApiKey = ApiKey,
            IngestionTimeoutSeconds = 5
        };
        configure?.Invoke(options);
        return new FastApiBrochureIngestionClient(new HttpClient(handler), Options.Create(options));
    }

    private static BrochureIngestionRequest Request()
        => new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "1",
            "uploads/product-brochures/brochure.pdf",
            new string('a', 64));

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json)
        => new(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _send;

        public StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send)
        {
            _send = send;
        }

        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return _send(request, cancellationToken);
        }
    }
}
