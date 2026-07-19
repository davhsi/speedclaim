using SpeedClaim.Api.Models.Enums;

namespace SpeedClaim.Api.Models;

public class SpeedyWorkspaceMessage
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public SpeedyWorkspaceMessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Intent { get; set; }
    public string? Risk { get; set; }
    public string? ActionsJson { get; set; }
    public string? Model { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public virtual SpeedyWorkspaceConversation Conversation { get; set; } = null!;
}
