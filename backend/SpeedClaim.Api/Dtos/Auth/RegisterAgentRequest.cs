namespace SpeedClaim.Api.Dtos.Auth;

public record RegisterAgentRequest(
    string Email,
    string Password,
    string FullName,
    string Phone,
    string Address,
    string LicenseNumber,
    string AgencyName
);
