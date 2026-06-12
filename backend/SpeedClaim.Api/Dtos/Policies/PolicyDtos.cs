using System;
using System.Collections.Generic;
using SpeedClaim.Api.Models.Enums;

namespace SpeedClaim.Api.Dtos.Policies;


public record EndorsementDto(
    Guid Id,
    Guid PolicyId,
    string EndorsementType,
    string Description,
    string? OldValue,
    string? NewValue,
    string Status,
    Guid RequestedById,
    Guid? ReviewedById,
    DateTimeOffset? ReviewedAt,
    DateTimeOffset CreatedAt
);

public record RequestEndorsementRequest(
    EndorsementType EndorsementType,
    string Description,
    string? OldValue,
    string? NewValue
);

public record ApproveRejectEndorsementRequest(
    bool IsApproved,
    string Reason
);

public record UpdateNomineeRequest(
    string FullName,
    string Relationship,
    DateOnly DateOfBirth,
    decimal SharePercentage,
    bool IsMinor,
    string? AppointeeName
);
