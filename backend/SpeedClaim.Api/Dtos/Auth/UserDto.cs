using System;

namespace SpeedClaim.Api.Dtos.Auth;

public record UserDto(
    Guid Id,
    string Email,
    string Salutation,
    string FirstName,
    string LastName,
    string FullName,
    string Phone,
    string Role,
    string MaritalStatus,
    Guid? CustomerId,
    bool IsEmailVerified,
    bool IsActive,
    DateTimeOffset CreatedAt,
    SpeedClaim.Api.Dtos.Common.AddressDto? PermanentAddress,
    SpeedClaim.Api.Dtos.Common.AddressDto? CurrentAddress
);
