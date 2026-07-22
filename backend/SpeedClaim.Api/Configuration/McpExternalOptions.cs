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
    /// Enables first-use identity linking only when an Auth0-issued MCP access token carries a
    /// verified email claim that exactly matches one eligible SpeedClaim customer.
    /// Disabled by default so existing deployments retain the explicit portal-link behaviour
    /// until the corresponding Auth0 Action is configured.
    /// </summary>
    public bool AutoLinkVerifiedEmail { get; init; }
    /// <summary>
    /// Auth0 application client used only by the customer-initiated account-linking flow.
    /// It is intentionally separate from dynamically registered MCP host clients.
    /// </summary>
    public string? AccountLinkClientId { get; init; }
    /// <summary>Secret for the server-side Auth0 account-linking application. Store only in Key Vault.</summary>
    public string? AccountLinkClientSecret { get; init; }

    /// <summary>
    /// Canonical OAuth resource identifier for the remote MCP server. It must match the
    /// transport URL the host connects to, not merely the API origin. OAuth clients cache a
    /// token by this value, so advertising the origin here while hosting MCP at <c>/mcp</c>
    /// creates two distinct credentials and causes a repeated Connect prompt.
    /// </summary>
    public string ResourceServerIdentifier => string.IsNullOrWhiteSpace(PublicBaseUrl)
        ? string.Empty
        : PublicBaseUrl.TrimEnd('/') + "/mcp";

    public void ValidateWhenEnabled()
    {
        if (!Enabled)
            return;

        if (!Uri.TryCreate(Issuer, UriKind.Absolute, out _)
            || !Uri.TryCreate(PublicBaseUrl, UriKind.Absolute, out _)
            || string.IsNullOrWhiteSpace(Audience))
            throw new InvalidOperationException(
                "Mcp:External requires Issuer, Audience, and PublicBaseUrl when Enabled is true.");

        if (!string.Equals(Audience, ResourceServerIdentifier, StringComparison.Ordinal))
            throw new InvalidOperationException(
                $"Mcp:External:Audience must exactly match the canonical MCP resource URL '{ResourceServerIdentifier}'. " +
                "Create an Auth0 API with that identifier and update the Key Vault audience before enabling the external MCP server.");
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

    /// <summary>
    /// Namespaced claims emitted by the Auth0 Post-Login Action for external MCP access tokens.
    /// The namespace is derived from the configured public resource origin so it remains owned
    /// by this resource server rather than an AI host.
    /// </summary>
    public string VerifiedEmailClaim => string.IsNullOrWhiteSpace(PublicBaseUrl)
        ? string.Empty
        : $"{PublicBaseUrl.TrimEnd('/')}/claims/email";

    public string EmailVerifiedClaim => string.IsNullOrWhiteSpace(PublicBaseUrl)
        ? string.Empty
        : $"{PublicBaseUrl.TrimEnd('/')}/claims/email_verified";
}
