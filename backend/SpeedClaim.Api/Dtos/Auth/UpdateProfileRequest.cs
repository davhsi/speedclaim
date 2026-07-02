namespace SpeedClaim.Api.Dtos.Auth;

public record UpdateProfileRequest(
    string FirstName,
    string LastName,
    string Phone,
    string? Salutation,
    string MaritalStatus,
    DateOnly? DateOfBirth = null
);
