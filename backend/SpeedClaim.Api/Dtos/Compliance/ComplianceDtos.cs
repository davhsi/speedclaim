using System;

namespace SpeedClaim.Api.Dtos.Compliance;

public class AuditLogDto
{
    public Guid Id { get; set; }
    public Guid? ActorId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UserConsentDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string ConsentType { get; set; } = string.Empty;
    public bool IsGranted { get; set; }
    public string ConsentVersion { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public DateTime GrantedAt { get; set; }
    public DateTime? WithdrawnAt { get; set; }
}

public class UpdateUserConsentRequest
{
    public string ConsentType { get; set; } = string.Empty;
    public bool IsGranted { get; set; }
}
