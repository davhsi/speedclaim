using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SpeedClaim.Api.Configuration;
using SpeedClaim.Api.Dtos.Catalog;
using SpeedClaim.Api.Exceptions;
using SpeedClaim.Api.Interfaces;

namespace SpeedClaim.Api.Services;

public sealed class FastApiBrochureIngestionClient : IBrochureIngestionClient
{
    private const string ApiKeyHeader = "X-Internal-Api-Key";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly AiServiceOptions _options;

    public FastApiBrochureIngestionClient(HttpClient httpClient, IOptions<AiServiceOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<BrochureIngestionResponse> IngestAsync(
        BrochureIngestionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(_options.BaseUrl, UriKind.Absolute, out var baseUri) ||
            baseUri.Scheme is not ("http" or "https"))
            throw new BrochureIngestionException("ai_configuration_invalid", "AI service base URL is not configured correctly.");
        if (string.IsNullOrWhiteSpace(_options.InternalApiKey) || _options.InternalApiKey.Length < 32)
            throw new BrochureIngestionException("ai_configuration_invalid", "AI service authentication is not configured.");
        if (_options.IngestionTimeoutSeconds is < 1 or > 300)
            throw new BrochureIngestionException("ai_configuration_invalid", "AI service ingestion timeout is invalid.");

        using var message = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri(baseUri, "/internal/v1/brochures/ingest"))
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        };
        message.Headers.TryAddWithoutValidation(ApiKeyHeader, _options.InternalApiKey);
        message.Headers.TryAddWithoutValidation("X-Correlation-ID", request.RequestId.ToString());

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(TimeSpan.FromSeconds(_options.IngestionTimeoutSeconds));

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, timeoutSource.Token);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new BrochureIngestionException("ai_ingestion_timeout", "AI brochure ingestion timed out.", exception);
        }
        catch (HttpRequestException exception)
        {
            throw new BrochureIngestionException("ai_ingestion_unavailable", "AI brochure ingestion is unavailable.", exception);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
                throw await CreateFailureAsync(response, timeoutSource.Token);

            BrochureIngestionResponse? result;
            try
            {
                result = await response.Content.ReadFromJsonAsync<BrochureIngestionResponse>(JsonOptions, timeoutSource.Token);
            }
            catch (Exception exception) when (exception is JsonException or NotSupportedException)
            {
                throw new BrochureIngestionException("invalid_ingestion_response", "AI brochure ingestion returned an invalid response.", exception);
            }

            if (result is null ||
                result.Status is not ("Succeeded" or "NoOp") ||
                result.PageCount <= 0 ||
                result.ParentChunkCount <= 0 ||
                result.ChildChunkCount <= 0 ||
                result.EmbeddingDimension <= 0 ||
                string.IsNullOrWhiteSpace(result.EmbeddingProvider) ||
                string.IsNullOrWhiteSpace(result.EmbeddingModel))
                throw new BrochureIngestionException("invalid_ingestion_response", "AI brochure ingestion returned incomplete metadata.");

            return result;
        }
    }

    private static async Task<BrochureIngestionException> CreateFailureAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        FastApiErrorEnvelope? envelope = null;
        try
        {
            envelope = await response.Content.ReadFromJsonAsync<FastApiErrorEnvelope>(JsonOptions, cancellationToken);
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            // Provider error bodies are untrusted; use the status mapping below.
        }

        var fallback = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "ai_ingestion_unauthorized",
            HttpStatusCode.RequestTimeout => "ai_ingestion_timeout",
            HttpStatusCode.TooManyRequests => "ai_ingestion_rate_limited",
            HttpStatusCode.ServiceUnavailable or HttpStatusCode.BadGateway or HttpStatusCode.GatewayTimeout
                => "ai_ingestion_unavailable",
            _ when (int)response.StatusCode >= 500 => "ai_ingestion_unavailable",
            _ => "ai_ingestion_rejected"
        };
        var providerCode = envelope?.Error?.Code;
        var safeCode = !string.IsNullOrWhiteSpace(providerCode) && providerCode.Length <= 100 &&
            providerCode.All(character => char.IsAsciiLetterOrDigit(character) || character == '_')
                ? providerCode
                : fallback;
        return new BrochureIngestionException(safeCode, "AI brochure ingestion did not succeed.");
    }

    private sealed record FastApiErrorEnvelope(FastApiError? Error);
    private sealed record FastApiError(string? Code);
}
