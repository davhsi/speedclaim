using SpeedClaim.Api.Models.Enums;

namespace SpeedClaim.Api.Models;

public class PolicyAssistantMessage
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public PolicyAssistantMessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? EvidenceStatus { get; set; }
    public string? CitationsJson { get; set; }
    public string? Model { get; set; }
    public string? PromptVersion { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public virtual PolicyAssistantConversation Conversation { get; set; } = null!;
}
