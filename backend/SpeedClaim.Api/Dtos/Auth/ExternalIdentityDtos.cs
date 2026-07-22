namespace SpeedClaim.Api.Dtos.Auth;

public record ExternalIdentityAuthorizationResponse(string AuthorizationUrl);

public record LinkedExternalIdentityDto(
    string Provider,
    DateTimeOffset LinkedAt);
