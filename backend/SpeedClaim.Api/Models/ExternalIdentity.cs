namespace SpeedClaim.Api.Models;

/// <summary>
/// A verified identity from an external authorization provider.
/// This is intentionally separate from first-party SpeedClaim sessions and passwords.
/// </summary>
public class ExternalIdentity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public DateTimeOffset LinkedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastAuthenticatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
