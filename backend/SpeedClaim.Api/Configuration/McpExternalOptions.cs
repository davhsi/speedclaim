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
}
