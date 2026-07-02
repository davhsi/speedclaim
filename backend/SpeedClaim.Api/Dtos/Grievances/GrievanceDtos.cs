using System;
using SpeedClaim.Api.Models.Enums;

namespace SpeedClaim.Api.Dtos.Grievances;

public record GrievanceDto(
    Guid Id,
    string GrievanceNumber,
    Guid CustomerId,
    Guid? PolicyId,
    Guid? ClaimId,
    string? PolicyNumber,
    string? ClaimNumber,
    string Category,
    string Description,
    string Status,
    Guid? AssignedToId,
    string? ResolutionNotes,
    DateTimeOffset? ResolvedAt,
    DateTimeOffset CreatedAt,
    string? AttachmentPath
);

public record RaiseGrievanceRequest(
    Guid? PolicyId,
    Guid? ClaimId,
    GrievanceCategory Category,
    string Description
);

public record UpdateGrievanceStatusRequest(
    GrievanceStatus Status,
    string? ResolutionNotes
);

public record AssignGrievanceRequest(
    Guid AssignedToId
);
