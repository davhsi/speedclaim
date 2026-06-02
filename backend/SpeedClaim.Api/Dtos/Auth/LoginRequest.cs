namespace SpeedClaim.Api.Dtos.Auth;

public record LoginRequest(
    string Email,
    string Password
);
