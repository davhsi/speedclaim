using System;

namespace SpeedClaim.Api.Models;

public class UserConsent
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    // What the user consented to: DataProcessing | KycDataCollection | Marketing
    public string ConsentType { get; set; } = string.Empty;
    public bool IsGranted { get; set; }
    public DateTimeOffset ConsentedAt { get; set; } = DateTimeOffset.UtcNow;

    // Version of the privacy policy / consent text shown
    public string ConsentVersion { get; set; } = "1.0";

    public string? IpAddress { get; set; }

    public bool IsRevoked { get; set; } = false;
    public DateTimeOffset? RevokedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
