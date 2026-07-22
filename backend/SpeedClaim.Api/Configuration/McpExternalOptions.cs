namespace SpeedClaim.Api.Configuration;

/// <summary>
/// Opt-in configuration for the public, read-only MCP resource server.
/// Keeping this disabled is the safe production default.
/// </summary>
public sealed class McpExternalOptions
{
    public const string SectionName = "Mcp:External";

    public bool Enabled { get; init; }
    public string? Issuer { get; init; }
    public string? Audience { get; init; }
    public string? PublicBaseUrl { get; init; }
    /// <summary>
    /// Auth0 application client used only by the customer-initiated account-linking flow.
    /// It is intentionally separate from dynamically registered MCP host clients.
    /// </summary>
    public string? AccountLinkClientId { get; init; }
    /// <summary>Secret for the server-side Auth0 account-linking application. Store only in Key Vault.</summary>
    public string? AccountLinkClientSecret { get; init; }

    /// <summary>
    /// Canonical OAuth resource-server identifier. This deliberately remains the public API
    /// origin (including its trailing slash); the MCP transport endpoint is hosted below it at
    /// <c>/mcp</c>.
    /// </summary>
    public string ResourceServerIdentifier => string.IsNullOrWhiteSpace(PublicBaseUrl)
        ? string.Empty
        : PublicBaseUrl.TrimEnd('/') + "/";

    public void ValidateWhenEnabled()
    {
        if (!Enabled)
            return;

        if (!Uri.TryCreate(Issuer, UriKind.Absolute, out _)
            || !Uri.TryCreate(PublicBaseUrl, UriKind.Absolute, out _)
            || string.IsNullOrWhiteSpace(Audience))
            throw new InvalidOperationException(
                "Mcp:External requires Issuer, Audience, and PublicBaseUrl when Enabled is true.");
    }

    public void ValidateAccountLinking()
    {
        if (!Enabled)
            throw new InvalidOperationException("External AI account linking is not enabled.");

        if (!Uri.TryCreate(Issuer, UriKind.Absolute, out _)
            || !Uri.TryCreate(PublicBaseUrl, UriKind.Absolute, out _)
            || string.IsNullOrWhiteSpace(AccountLinkClientId)
            || string.IsNullOrWhiteSpace(AccountLinkClientSecret))
            throw new InvalidOperationException(
                "External AI account linking requires Mcp:External:Issuer, PublicBaseUrl, AccountLinkClientId, and AccountLinkClientSecret.");
    }

    public string AccountLinkRedirectUri => string.IsNullOrWhiteSpace(PublicBaseUrl)
        ? string.Empty
        : $"{PublicBaseUrl.TrimEnd('/')}/api/v1/users/external-identities/auth0/callback";
}
