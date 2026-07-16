using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SpeedClaim.Api.Configuration;
using SpeedClaim.Api.Dtos.Policies;
using SpeedClaim.Api.Exceptions;
using SpeedClaim.Api.Interfaces;

namespace SpeedClaim.Api.Services;

public sealed class FastApiPolicyQaClient : IPolicyQaClient
{
    private const string ApiKeyHeader = "X-Internal-Api-Key";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly AiServiceOptions _options;

    public FastApiPolicyQaClient(HttpClient httpClient, IOptions<AiServiceOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<PolicyQaResponse> AnswerAsync(PolicyQaRequest request, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(_options.BaseUrl, UriKind.Absolute, out var baseUri) || baseUri.Scheme is not ("http" or "https") ||
            string.IsNullOrWhiteSpace(_options.InternalApiKey) || _options.InternalApiKey.Length < 32)
            throw new BrochureIngestionException("ai_configuration_invalid", "Policy Guide is not configured.");
        if (request.Question.Length > _options.PolicyQaMaxQuestionCharacters)
            throw new ValidationException("Question is too long.");

        using var message = new HttpRequestMessage(HttpMethod.Post, new Uri(baseUri, "/internal/v1/policy-qa"))
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        };
        message.Headers.TryAddWithoutValidation(ApiKeyHeader, _options.InternalApiKey);
        message.Headers.TryAddWithoutValidation("X-Correlation-ID", request.RequestId.ToString());
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(_options.PolicyQaTimeoutSeconds));
        try
        {
            using var response = await _httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                var code = response.StatusCode switch
                {
                    HttpStatusCode.TooManyRequests => "policy_qa_rate_limited",
                    HttpStatusCode.RequestTimeout => "policy_qa_timeout",
                    HttpStatusCode.ServiceUnavailable or HttpStatusCode.BadGateway or HttpStatusCode.GatewayTimeout => "policy_qa_unavailable",
                    _ when (int)response.StatusCode >= 500 => "policy_qa_unavailable",
                    _ => "policy_qa_rejected"
                };
                throw new BrochureIngestionException(code, "Policy Guide is temporarily unavailable.");
            }
            var result = await response.Content.ReadFromJsonAsync<PolicyQaResponse>(JsonOptions, timeout.Token);
            if (result is null || result.RequestId != request.RequestId || result.BrochureVersion != request.BrochureVersion ||
                string.IsNullOrWhiteSpace(result.Answer) || result.Citations.Any(c => c.PageNumber < 1 || string.IsNullOrWhiteSpace(c.Excerpt)))
                throw new BrochureIngestionException("invalid_policy_qa_response", "Policy Guide returned an invalid response.");
            return result;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new BrochureIngestionException("policy_qa_timeout", "Policy Guide timed out.");
        }
        catch (HttpRequestException exception)
        {
            throw new BrochureIngestionException("policy_qa_unavailable", "Policy Guide is temporarily unavailable.", exception);
        }
    }
}
