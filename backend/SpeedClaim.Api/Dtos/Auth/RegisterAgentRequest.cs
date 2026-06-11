using SpeedClaim.Api.Dtos.Common;

namespace SpeedClaim.Api.Dtos.Auth;

public record RegisterAgentRequest(
    string Email,
    string Password,
    string Salutation,
    string FirstName,
    string LastName,
    string Phone,
    AddressDto PermanentAddress,
    AddressDto? CurrentAddress,
    bool IsSameAsPermanent,
    string LicenseNumber,
    string AgencyName,
    string AadhaarNumber,
    string PanNumber,
    SpeedClaim.Api.Models.Enums.MaritalStatus MaritalStatus
);
