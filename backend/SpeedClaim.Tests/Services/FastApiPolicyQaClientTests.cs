using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using SpeedClaim.Api.Configuration;
using SpeedClaim.Api.Dtos.Policies;
using SpeedClaim.Api.Exceptions;
using SpeedClaim.Api.Services;

namespace SpeedClaim.Tests.Services;

[TestFixture]
public class FastApiPolicyQaClientTests
{
    private const string ApiKey = "test-only-internal-key-that-is-long-enough";

    [Test]
    public async Task AnswerAsync_SendsOnlyBrochureQuestionContract()
    {
        string? body = null;
        var request = Request();
        var handler = new StubHandler(async (message, _) =>
        {
            body = await message.Content!.ReadAsStringAsync();
            return Json(HttpStatusCode.OK, $$"""{"requestId":"{{request.RequestId}}","answer":"A grounded answer.","evidenceStatus":"Grounded","brochureVersion":"1","citations":[{"index":1,"pageNumber":6,"sectionTitle":"Waiting period","clauseReference":"4.1","excerpt":"Evidence"}],"promptVersion":"v1","provider":"Fake","model":"fake"}""");
        });

        var result = await Client(handler).AnswerAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.EvidenceStatus, Is.EqualTo("Grounded"));
            Assert.That(body, Does.Contain("brochureId"));
            Assert.That(body, Does.Not.Contain("policyId"));
            Assert.That(body, Does.Not.Contain("customer"));
        });
    }

    [Test]
    public void AnswerAsync_RateLimit_IsMappedToFeatureError()
    {
        var exception = Assert.ThrowsAsync<BrochureIngestionException>(() => Client(new StubHandler((_, _) => Task.FromResult(Json(HttpStatusCode.TooManyRequests, "{}")))).AnswerAsync(Request()));
        Assert.That(exception!.ErrorCode, Is.EqualTo("policy_qa_rate_limited"));
    }

    [Test]
    public void AnswerAsync_InvalidResponse_IsRejected()
    {
        var request = Request();
        var exception = Assert.ThrowsAsync<BrochureIngestionException>(() => Client(new StubHandler((_, _) => Task.FromResult(Json(HttpStatusCode.OK, $$"""{"requestId":"{{request.RequestId}}","answer":"","evidenceStatus":"Grounded","brochureVersion":"1","citations":[],"promptVersion":"v1"}""")))).AnswerAsync(request));
        Assert.That(exception!.ErrorCode, Is.EqualTo("invalid_policy_qa_response"));
    }

    private static FastApiPolicyQaClient Client(HttpMessageHandler handler) => new(new HttpClient(handler), Options.Create(new AiServiceOptions { BaseUrl = "http://127.0.0.1:8000", InternalApiKey = ApiKey, PolicyQaTimeoutSeconds = 5 }));
    private static PolicyQaRequest Request() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "1", "What is the waiting period?");
    private static HttpResponseMessage Json(HttpStatusCode status, string json) => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    private sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request, cancellationToken); }
}
