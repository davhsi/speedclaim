using System;

namespace SpeedClaim.Api.Dtos.Claims;

public record ClaimResponse(
    Guid Id,
    string ClaimNumber,
    string Status,
    decimal ClaimedAmount,
    DateTime CreatedAt
);
