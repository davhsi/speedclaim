namespace SpeedClaim.Api.Contracts;

/// <summary>
/// Rendered email payload placed on Service Bus. Attachments are intentionally excluded because
/// they can exceed Service Bus message limits and may contain customer documents.
/// </summary>
public sealed record EmailDispatchMessage(
    string To,
    string Subject,
    string HtmlBody,
    DateTimeOffset QueuedAt);
