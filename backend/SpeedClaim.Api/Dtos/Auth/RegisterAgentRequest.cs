using SpeedClaim.Api.Dtos.Common;

namespace SpeedClaim.Api.Dtos.Auth;

public record RegisterAgentRequest(
    string Email,
    string Password,
    string FullName,
    string Phone,
    AddressDto Address,
    string LicenseNumber,
    string AgencyName,
    string AadhaarNumber,
    string PanNumber
);
