namespace SpeedClaim.Api.Models;

public class PolicyAssistantConversation
{
    public Guid Id { get; set; }
    public Guid PolicyId { get; set; }
    public Guid BrochureId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset RetainUntil { get; set; }

    public virtual Policy Policy { get; set; } = null!;
    public virtual ProductBrochure Brochure { get; set; } = null!;
    public virtual User CreatedBy { get; set; } = null!;
    public virtual ICollection<PolicyAssistantMessage> Messages { get; set; } = new List<PolicyAssistantMessage>();
}
