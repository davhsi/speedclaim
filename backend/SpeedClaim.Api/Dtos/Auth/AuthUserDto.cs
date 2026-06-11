using System;

namespace SpeedClaim.Api.Dtos.Auth;

public record AuthUserDto(
    Guid Id,
    string Email,
    string Salutation,
    string FirstName,
    string LastName,
    string FullName,
    string Phone,
    string Role,
    string MaritalStatus
);
