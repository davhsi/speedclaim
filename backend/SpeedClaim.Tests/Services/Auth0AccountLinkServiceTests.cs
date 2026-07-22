using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using SpeedClaim.Api.Configuration;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Services;

namespace SpeedClaim.Tests.Services;

[TestFixture]
public class Auth0AccountLinkServiceTests
{
    private Mock<IExternalIdentityService> _externalIdentities = null!;
    private MemoryDistributedCache _cache = null!;
    private Auth0AccountLinkService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _externalIdentities = new Mock<IExternalIdentityService>();
        _cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var handler = new StubHttpMessageHandler(request =>
        {
            if (request.RequestUri!.AbsolutePath == "/oauth/token")
                return Json("{\"access_token\":\"access-token\"}");
            if (request.RequestUri.AbsolutePath == "/userinfo")
                return Json("{\"sub\":\"auth0|customer-123\"}");
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(factory => factory.CreateClient(nameof(Auth0AccountLinkService)))
            .Returns(new HttpClient(handler));

        var options = Options.Create(new McpExternalOptions
        {
            Enabled = true,
            Issuer = "https://tenant.example.auth0.com/",
            PublicBaseUrl = "https://api.speedclaim.example",
            AccountLinkClientId = "account-link-client",
            AccountLinkClientSecret = "account-link-secret"
        });
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["FrontendUrl"] = "https://app.speedclaim.example"
        }).Build();
        _service = new Auth0AccountLinkService(
            _cache,
            httpClientFactory.Object,
            _externalIdentities.Object,
            options,
            configuration,
            Mock.Of<Microsoft.Extensions.Logging.ILogger<Auth0AccountLinkService>>());
    }

    [Test]
    public async Task StartAsync_UsesPkceAndReturnsAnOpaqueAuth0AuthorizationUrl()
    {
        var response = await _service.StartAsync(Guid.NewGuid());
        var uri = new Uri(response.AuthorizationUrl);
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);

        Assert.That(uri.AbsolutePath, Is.EqualTo("/authorize"));
        Assert.That(query["response_type"].ToString(), Is.EqualTo("code"));
        Assert.That(query["code_challenge_method"].ToString(), Is.EqualTo("S256"));
        Assert.That(query["redirect_uri"].ToString(), Is.EqualTo("https://api.speedclaim.example/api/v1/users/external-identities/auth0/callback"));
        Assert.That(query["state"].ToString(), Has.Length.GreaterThan(30));
        Assert.That(response.AuthorizationUrl, Does.Not.Contain("account-link-secret"));
    }

    [Test]
    public async Task CompleteAsync_ExchangesTheCodeAndLinksTheVerifiedAuth0Subject()
    {
        var userId = Guid.NewGuid();
        var response = await _service.StartAsync(userId);
        var state = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(new Uri(response.AuthorizationUrl).Query)["state"].ToString();

        var returnUrl = await _service.CompleteAsync("authorization-code", state, null);

        Assert.That(returnUrl, Is.EqualTo("https://app.speedclaim.example/profile?externalLink=success"));
        _externalIdentities.Verify(service => service.LinkAuth0SubjectAsync(userId, "auth0|customer-123"), Times.Once);
    }

    private static HttpResponseMessage Json(string body) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
    };

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responder(request));
    }
}
