using System;

namespace SpeedClaim.Api.Dtos.Auth;

public record UserDto(
    Guid Id,
    string Email,
    string FullName,
    string Phone,
    string Role
);
