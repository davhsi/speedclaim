using System;

namespace SpeedClaim.Api.Models;

public class EmailLog
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    
    public string RecipientEmail { get; set; } = string.Empty;
    public string TemplateKey { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    
    public string? VariablesUsed { get; set; } // JSONB
    
    public string Status { get; set; } = "QUEUED"; // queued, sent, failed, bounced
    public string? ProviderMessageId { get; set; }
    public string? ErrorMessage { get; set; }
    
    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public virtual User? User { get; set; }
}
