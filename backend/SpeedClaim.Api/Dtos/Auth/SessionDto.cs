using System;

namespace SpeedClaim.Api.Dtos.Auth;

public record SessionDto(
    Guid Id,
    Guid UserId,
    string UserEmail,
    string IpAddress,
    string UserAgent,
    DateTime ExpiresAt,
    bool IsRevoked,
    DateTimeOffset CreatedAt
);
