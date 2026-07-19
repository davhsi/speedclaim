using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SpeedClaim.Api.Configuration;
using SpeedClaim.Api.Dtos.Assistant;
using SpeedClaim.Api.Exceptions;
using SpeedClaim.Api.Interfaces;

namespace SpeedClaim.Api.Services;

public sealed class FastApiSpeedyWorkspaceClient : ISpeedyWorkspaceClient
{
    private const string ApiKeyHeader = "X-Internal-Api-Key";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly AiServiceOptions _options;

    public FastApiSpeedyWorkspaceClient(HttpClient httpClient, IOptions<AiServiceOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<SpeedyWorkspaceResponse> AnswerAsync(SpeedyWorkspaceRequest request, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(_options.BaseUrl, UriKind.Absolute, out var baseUri) || baseUri.Scheme is not ("http" or "https") ||
            string.IsNullOrWhiteSpace(_options.InternalApiKey) || _options.InternalApiKey.Length < 32)
            throw new BrochureIngestionException("ai_configuration_invalid", "Speedy is not configured.");

        using var message = new HttpRequestMessage(HttpMethod.Post, new Uri(baseUri, "/internal/v1/workspace"))
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        };
        message.Headers.TryAddWithoutValidation(ApiKeyHeader, _options.InternalApiKey);
        message.Headers.TryAddWithoutValidation("X-Correlation-ID", request.RequestId.ToString());
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(_options.WorkspaceTimeoutSeconds));
        try
        {
            using var response = await _httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                var code = response.StatusCode switch
                {
                    HttpStatusCode.TooManyRequests => "workspace_rate_limited",
                    HttpStatusCode.RequestTimeout => "workspace_timeout",
                    HttpStatusCode.ServiceUnavailable or HttpStatusCode.BadGateway or HttpStatusCode.GatewayTimeout => "workspace_unavailable",
                    _ when (int)response.StatusCode >= 500 => "workspace_unavailable",
                    _ => "workspace_rejected"
                };
                throw new BrochureIngestionException(code, "Speedy is temporarily unavailable.");
            }
            var result = await response.Content.ReadFromJsonAsync<SpeedyWorkspaceResponse>(JsonOptions, timeout.Token);
            if (result is null || result.RequestId != request.RequestId || string.IsNullOrWhiteSpace(result.Answer) ||
                string.IsNullOrWhiteSpace(result.Intent) || result.Actions.Any(a => string.IsNullOrWhiteSpace(a.Kind) || string.IsNullOrWhiteSpace(a.Label)))
                throw new BrochureIngestionException("invalid_workspace_response", "Speedy returned an invalid response.");
            return result;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new BrochureIngestionException("workspace_timeout", "Speedy took too long to reply. Please try again.");
        }
        catch (HttpRequestException exception)
        {
            throw new BrochureIngestionException("workspace_unavailable", "Speedy is temporarily unavailable.", exception);
        }
    }
}
