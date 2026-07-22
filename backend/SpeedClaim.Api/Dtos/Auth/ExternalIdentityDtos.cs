namespace SpeedClaim.Api.Dtos.Auth;

public record ExternalIdentityLinkCodeResponse(
    string Provider,
    string LinkCode,
    DateTime ExpiresAt);

public record LinkedExternalIdentityDto(
    string Provider,
    DateTimeOffset LinkedAt);
