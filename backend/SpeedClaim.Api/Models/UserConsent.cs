using System;

namespace SpeedClaim.Api.Models;

public class UserConsent
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string ConsentType { get; set; } = string.Empty;
    public bool IsGranted { get; set; }
    public string ConsentVersion { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
    public DateTime? WithdrawnAt { get; set; }

    // Navigation Properties
    public virtual User User { get; set; } = null!;
}
