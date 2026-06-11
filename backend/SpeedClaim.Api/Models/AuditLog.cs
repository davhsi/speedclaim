using System;

namespace SpeedClaim.Api.Models;

public class AuditLog
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? OldValue { get; set; } // Stored as JSON string/JSONB
    public string? NewValue { get; set; } // Stored as JSON string/JSONB
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public virtual User? User { get; set; }
}
