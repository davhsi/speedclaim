using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SpeedClaim.Api.Configuration;
using SpeedClaim.Api.Exceptions;
using SpeedClaim.Api.Interfaces;

namespace SpeedClaim.Api.Mcp;

public static class McpEndpointRouteBuilderExtensions
{
    public const string AuthScheme = "Auth0Mcp";
    private const string AccountRead = "speedclaim.account.read";
    private const string CatalogRead = "speedclaim.catalog.read";

    private static readonly string[] AccountTools =
    [
        "get_my_kyc_next_step", "get_my_policy_summary", "get_my_proposal_status", "get_my_next_premium_due",
        "get_my_claim_status", "get_my_grievance_status", "get_customer_assistance"
    ];

    public static WebApplication MapExternalMcp(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<McpExternalOptions>>().Value;
        if (!options.Enabled)
            return app;

        options.ValidateWhenEnabled();
        app.MapGet("/.well-known/oauth-protected-resource", () => Results.Ok(new
        {
            resource = options.ResourceServerIdentifier,
            authorization_servers = new[] { options.Issuer },
            scopes_supported = new[] { CatalogRead, AccountRead },
            bearer_methods_supported = new[] { "header" }
        })).AllowAnonymous();
        app.MapPost("/mcp", HandleAsync).AllowAnonymous();
        return app;
    }

    private static async Task<IResult> HandleAsync(HttpContext context, McpReadOnlyToolService tools, IExternalIdentityService identities, IOptions<McpExternalOptions> options)
    {
        JsonDocument request;
        try { request = await JsonDocument.ParseAsync(context.Request.Body, cancellationToken: context.RequestAborted); }
        catch (JsonException) { return Results.Json(Error(default, -32700, "Parse error")); }

        using (request)
        {
            var root = request.RootElement;
            var id = root.TryGetProperty("id", out var idElement) ? idElement.Clone() : default;
            if (!root.TryGetProperty("method", out var methodElement) || methodElement.ValueKind != JsonValueKind.String)
                return Results.Json(Error(id, -32600, "Invalid Request"));

            var method = methodElement.GetString();
            if (method == "notifications/initialized")
                return Results.Accepted();
            if (method == "initialize")
                return Results.Json(Result(id, new
                {
                    protocolVersion = "2025-06-18",
                    capabilities = new { tools = new { } },
                    serverInfo = new { name = "speedclaim", version = "1.0.0" },
                    instructions = "SpeedClaim is read-only in external AI hosts. Do not use it for payments, claim submission, KYC uploads, applications, or any consequential action."
                }));
            if (method == "ping")
                return Results.Json(Result(id, new { }));
            if (method == "tools/list")
                return Results.Json(Result(id, new { tools = ToolDefinitions() }));
            if (method != "tools/call")
                return Results.Json(Error(id, -32601, "Method not found"));

            var parameters = root.TryGetProperty("params", out var parametersElement) ? parametersElement : default;
            var toolName = parameters.ValueKind == JsonValueKind.Object && parameters.TryGetProperty("name", out var nameElement)
                ? nameElement.GetString() : null;
            if (string.IsNullOrWhiteSpace(toolName) || !AllToolNames.Contains(toolName, StringComparer.Ordinal))
                return Results.Json(Error(id, -32602, "Unknown tool"));

            var authenticate = await context.AuthenticateAsync(AuthScheme);
            if (!authenticate.Succeeded || authenticate.Principal is null)
                return Results.Json(Result(id, AuthenticationRequired(options.Value)), statusCode: StatusCodes.Status200OK);

            var requiredScope = CatalogTools.Contains(toolName, StringComparer.Ordinal) ? CatalogRead : AccountRead;
            if (!HasScope(authenticate.Principal, requiredScope))
                return Results.Json(Result(id, ToolError($"The Auth0 token is missing the required permission: {requiredScope}.")), statusCode: StatusCodes.Status200OK);

            var subject = authenticate.Principal.FindFirstValue("sub");
            if (string.IsNullOrWhiteSpace(subject))
                return Results.Json(Result(id, ToolError("The Auth0 token does not contain a subject.")), statusCode: StatusCodes.Status200OK);

            var args = parameters.ValueKind == JsonValueKind.Object && parameters.TryGetProperty("arguments", out var argsElement)
                ? argsElement.Clone() : EmptyArguments;
            var userId = await identities.ResolveActiveUserIdAsync("Auth0", subject);
            var clientId = authenticate.Principal.FindFirstValue("azp") ?? authenticate.Principal.FindFirstValue("client_id");
            try
            {
                var data = await tools.ExecuteAsync(toolName, userId, args, subject);
                await tools.AuditInvocationAsync(userId, toolName, subject, clientId, requiredScope, "success");
                return Results.Json(Result(id, ToolSuccess(data)));
            }
            catch (Exception exception) when (exception is ValidationException or ForbiddenException or ConflictException)
            {
                await tools.AuditInvocationAsync(userId, toolName, subject, clientId, requiredScope, "rejected");
                return Results.Json(Result(id, ToolError(exception.Message)), statusCode: StatusCodes.Status200OK);
            }
        }
    }

    private static object[] ToolDefinitions() =>
    [
        Tool("get_available_products", "List published SpeedClaim insurance products.", CatalogRead),
        Tool("select_published_brochure", "List safe metadata for published product brochures; optionally filter by productName.", CatalogRead, new { type = "object", properties = new { productName = new { type = "string" } } }),
        Tool("get_my_kyc_next_step", "Get the linked customer's KYC status and safe next step.", AccountRead),
        Tool("get_my_policy_summary", "Get summaries of the linked customer's policies.", AccountRead),
        Tool("get_my_proposal_status", "Get statuses of the linked customer's proposals.", AccountRead),
        Tool("get_my_next_premium_due", "Get upcoming premium installments for the linked customer.", AccountRead),
        Tool("get_my_claim_status", "Get claim statuses for the linked customer.", AccountRead),
        Tool("get_my_grievance_status", "Get grievance statuses for the linked customer.", AccountRead),
        Tool("get_customer_assistance", "Explain the safe limits of this read-only SpeedClaim connector.", AccountRead)
    ];

    private static object Tool(string name, string description, string scope, object? inputSchema = null) => new
    {
        name,
        title = name.Replace('_', ' '),
        description,
        inputSchema = inputSchema ?? new { type = "object", properties = new { } },
        securitySchemes = new[] { new { type = "oauth2", scopes = new[] { scope } } }
    };

    private static readonly string[] CatalogTools = ["get_available_products", "select_published_brochure"];
    private static readonly string[] AllToolNames = [.. CatalogTools, .. AccountTools];
    private static readonly JsonElement EmptyArguments = JsonDocument.Parse("{}").RootElement.Clone();

    private static bool HasScope(ClaimsPrincipal principal, string requiredScope) => principal.Claims
        .Where(claim => claim.Type is "scope" or "permissions")
        .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        .Contains(requiredScope, StringComparer.Ordinal);

    private static object ToolSuccess(object data) => new
    {
        content = new[] { new { type = "text", text = JsonSerializer.Serialize(data) } },
        structuredContent = data
    };

    private static object ToolError(string message) => new
    {
        content = new[] { new { type = "text", text = message } },
        isError = true
    };

    private static object AuthenticationRequired(McpExternalOptions options) => new
    {
        content = new[] { new { type = "text", text = "Authentication required. Sign in with Auth0 to continue." } },
        isError = true,
        _meta = new Dictionary<string, string[]>
        {
            ["mcp/www_authenticate"] = [$"Bearer resource_metadata=\"{options.PublicBaseUrl!.TrimEnd('/')}/.well-known/oauth-protected-resource\", error=\"invalid_token\", error_description=\"Sign in to SpeedClaim\""]
        }
    };

    private static object Result(JsonElement id, object result) => new { jsonrpc = "2.0", id = ToId(id), result };
    private static object Error(JsonElement id, int code, string message) => new { jsonrpc = "2.0", id = ToId(id), error = new { code, message } };
    private static object? ToId(JsonElement id) => id.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null ? null : id;
}
