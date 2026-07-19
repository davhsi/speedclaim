namespace SpeedClaim.Api.Models;

public class SpeedyWorkspaceConversation
{
    public Guid Id { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string Title { get; set; } = "New conversation";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset RetainUntil { get; set; }

    public virtual User CreatedBy { get; set; } = null!;
    public virtual ICollection<SpeedyWorkspaceMessage> Messages { get; set; } = new List<SpeedyWorkspaceMessage>();
}
