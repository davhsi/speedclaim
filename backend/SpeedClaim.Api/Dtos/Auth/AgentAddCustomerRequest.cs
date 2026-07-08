using System;
using SpeedClaim.Api.Dtos.Common;
using SpeedClaim.Api.Models.Enums;

namespace SpeedClaim.Api.Dtos.Auth;

public record AgentAddCustomerRequest(
    string Email,
    string Salutation,
    string FirstName,
    string LastName,
    string Phone,
    AddressDto PermanentAddress,
    AddressDto? CurrentAddress,
    bool IsSameAsPermanent,
    DateOnly DateOfBirth,
    Gender Gender,
    MaritalStatus MaritalStatus
);
