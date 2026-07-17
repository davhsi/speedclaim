using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SpeedClaim.Api.Configuration;
using SpeedClaim.Api.Dtos.Assistant;
using SpeedClaim.Api.Exceptions;
using SpeedClaim.Api.Interfaces;

namespace SpeedClaim.Api.Services;

public sealed class FastApiSpeedyAssistantClient : ISpeedyAssistantClient
{
    private const string ApiKeyHeader = "X-Internal-Api-Key";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly AiServiceOptions _options;

    public FastApiSpeedyAssistantClient(HttpClient httpClient, IOptions<AiServiceOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<SpeedyAssistantResponse> AnswerAsync(SpeedyAssistantRequest request, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(_options.BaseUrl, UriKind.Absolute, out var baseUri) || baseUri.Scheme is not ("http" or "https") ||
            string.IsNullOrWhiteSpace(_options.InternalApiKey) || _options.InternalApiKey.Length < 32)
            throw new BrochureIngestionException("ai_configuration_invalid", "Speedy is not configured.");

        using var message = new HttpRequestMessage(HttpMethod.Post, new Uri(baseUri, "/internal/v1/speedy"))
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
                    HttpStatusCode.TooManyRequests => "speedy_rate_limited",
                    HttpStatusCode.RequestTimeout => "speedy_timeout",
                    HttpStatusCode.ServiceUnavailable or HttpStatusCode.BadGateway or HttpStatusCode.GatewayTimeout => "speedy_unavailable",
                    _ when (int)response.StatusCode >= 500 => "speedy_unavailable",
                    _ => "speedy_rejected"
                };
                throw new BrochureIngestionException(code, "Speedy is temporarily unavailable.");
            }
            var result = await response.Content.ReadFromJsonAsync<SpeedyAssistantResponse>(JsonOptions, timeout.Token);
            if (result is null || result.RequestId != request.RequestId || string.IsNullOrWhiteSpace(result.Answer))
                throw new BrochureIngestionException("invalid_speedy_response", "Speedy returned an invalid response.");
            return result;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new BrochureIngestionException("speedy_timeout", "Speedy took too long to reply. Please try again.");
        }
        catch (HttpRequestException exception)
        {
            throw new BrochureIngestionException("speedy_unavailable", "Speedy is temporarily unavailable.", exception);
        }
    }
}
