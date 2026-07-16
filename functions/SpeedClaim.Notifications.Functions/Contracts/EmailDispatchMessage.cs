namespace SpeedClaim.Notifications.Functions.Contracts;

public sealed record EmailDispatchMessage(
    string To,
    string Subject,
    string HtmlBody,
    DateTimeOffset QueuedAt);
