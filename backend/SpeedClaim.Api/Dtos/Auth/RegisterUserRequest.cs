namespace SpeedClaim.Api.Dtos.Auth;

public record RegisterUserRequest(
    string Email,
    string Password,
    string FullName,
    string Phone,
    string Address
);
