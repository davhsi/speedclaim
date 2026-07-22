using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using SpeedClaim.Api.Configuration;
using SpeedClaim.Api.Dtos.Auth;
using SpeedClaim.Api.Exceptions;
using SpeedClaim.Api.Interfaces;

namespace SpeedClaim.Api.Services;

public sealed class Auth0AccountLinkService : IAuth0AccountLinkService
{
    private const string CachePrefix = "external-identity-auth0-link:";
    private static readonly TimeSpan TransactionLifetime = TimeSpan.FromMinutes(10);
    private readonly IDistributedCache _cache;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IExternalIdentityService _externalIdentities;
    private readonly McpExternalOptions _options;
    private readonly string _frontendUrl;
    private readonly ILogger<Auth0AccountLinkService> _logger;

    public Auth0AccountLinkService(
        IDistributedCache cache,
        IHttpClientFactory httpClientFactory,
        IExternalIdentityService externalIdentities,
        IOptions<McpExternalOptions> options,
        IConfiguration configuration,
        ILogger<Auth0AccountLinkService> logger)
    {
        _cache = cache;
        _httpClientFactory = httpClientFactory;
        _externalIdentities = externalIdentities;
        _options = options.Value;
        _frontendUrl = configuration["FrontendUrl"]?.TrimEnd('/') ?? string.Empty;
        _logger = logger;
    }

    public async Task<ExternalIdentityAuthorizationResponse> StartAsync(Guid userId)
    {
        _options.ValidateAccountLinking();
        if (!Uri.TryCreate(_frontendUrl, UriKind.Absolute, out _))
            throw new InvalidOperationException("External AI account linking requires a valid FrontendUrl.");

        var state = RandomUrlSafeValue();
        var codeVerifier = RandomUrlSafeValue();
        var codeChallenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)));
        var transaction = new AccountLinkTransaction(userId, codeVerifier, DateTimeOffset.UtcNow.Add(TransactionLifetime));
        await _cache.SetStringAsync(CachePrefix + state, JsonSerializer.Serialize(transaction), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TransactionLifetime
        });

        var issuer = _options.Issuer!.TrimEnd('/');
        var query = new Dictionary<string, string?>
        {
            ["response_type"] = "code",
            ["client_id"] = _options.AccountLinkClientId,
            ["redirect_uri"] = _options.AccountLinkRedirectUri,
            ["scope"] = "openid",
            ["state"] = state,
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256"
        };

        return new ExternalIdentityAuthorizationResponse(QueryHelpers.AddQueryString($"{issuer}/authorize", query));
    }

    public async Task<string> CompleteAsync(string? code, string? state, string? error)
    {
        try
        {
            _options.ValidateAccountLinking();
            if (string.IsNullOrWhiteSpace(state) || !string.IsNullOrWhiteSpace(error) || string.IsNullOrWhiteSpace(code))
                return ReturnToProfile("failed");

            var cacheKey = CachePrefix + state;
            var serializedTransaction = await _cache.GetStringAsync(cacheKey);
            await _cache.RemoveAsync(cacheKey);
            var transaction = serializedTransaction is null ? null : JsonSerializer.Deserialize<AccountLinkTransaction>(serializedTransaction);
            if (transaction is null || transaction.ExpiresAt <= DateTimeOffset.UtcNow)
                return ReturnToProfile("expired");

            var subject = await ExchangeCodeForSubjectAsync(code, transaction.CodeVerifier);
            await _externalIdentities.LinkAuth0SubjectAsync(transaction.UserId, subject);
            _logger.LogInformation("Completed customer-initiated Auth0 identity link for SpeedClaim user {UserId}", transaction.UserId);
            return ReturnToProfile("success");
        }
        catch (Exception exception) when (exception is ValidationException or ForbiddenException or ConflictException or HttpRequestException or JsonException or InvalidOperationException)
        {
            _logger.LogWarning(exception, "Auth0 account-link callback was rejected");
            return ReturnToProfile("failed");
        }
    }

    private async Task<string> ExchangeCodeForSubjectAsync(string code, string codeVerifier)
    {
        var issuer = _options.Issuer!.TrimEnd('/');
        var client = _httpClientFactory.CreateClient(nameof(Auth0AccountLinkService));
        using var tokenResponse = await client.PostAsync($"{issuer}/oauth/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = _options.AccountLinkClientId!,
            ["client_secret"] = _options.AccountLinkClientSecret!,
            ["code"] = code,
            ["redirect_uri"] = _options.AccountLinkRedirectUri,
            ["code_verifier"] = codeVerifier
        }));
        tokenResponse.EnsureSuccessStatusCode();

        using var tokenPayload = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync());
        if (!tokenPayload.RootElement.TryGetProperty("access_token", out var accessTokenElement)
            || string.IsNullOrWhiteSpace(accessTokenElement.GetString()))
            throw new ValidationException("Auth0 did not return an access token for account linking.");

        using var userInfoRequest = new HttpRequestMessage(HttpMethod.Get, $"{issuer}/userinfo");
        userInfoRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessTokenElement.GetString());
        using var userInfoResponse = await client.SendAsync(userInfoRequest);
        userInfoResponse.EnsureSuccessStatusCode();
        using var userInfoPayload = JsonDocument.Parse(await userInfoResponse.Content.ReadAsStringAsync());
        var subject = userInfoPayload.RootElement.TryGetProperty("sub", out var subjectElement) ? subjectElement.GetString() : null;
        if (string.IsNullOrWhiteSpace(subject))
            throw new ValidationException("Auth0 did not return a subject for account linking.");

        return subject;
    }

    private string ReturnToProfile(string outcome)
    {
        var frontend = string.IsNullOrWhiteSpace(_frontendUrl) ? "/profile" : $"{_frontendUrl}/profile";
        return QueryHelpers.AddQueryString(frontend, "externalLink", outcome);
    }

    private static string RandomUrlSafeValue() => Base64Url(RandomNumberGenerator.GetBytes(32));
    private static string Base64Url(byte[] value) => WebEncoders.Base64UrlEncode(value);

    private sealed record AccountLinkTransaction(Guid UserId, string CodeVerifier, DateTimeOffset ExpiresAt);
}
