using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using SpeedClaim.Api.Models.Enums;

namespace SpeedClaim.Api.Dtos.Claims;

public record IntimateClaimRequest(
    Guid PolicyId,
    Guid? ClaimantMemberId,
    ClaimType ClaimType,
    decimal ClaimAmountRequested,
    bool IsCashless,
    DateTime IncidentDate,
    string IncidentDescription
);

public record ClaimDto(
    Guid Id,
    string ClaimNumber,
    Guid PolicyId,
    Guid CustomerId,
    Guid? ClaimantMemberId,
    string ClaimType,
    decimal ClaimAmountRequested,
    decimal? ClaimAmountApproved,
    bool IsCashless,
    string Status,
    DateTime IntimationDate,
    DateTime IncidentDate,
    string IncidentDescription,
    Guid? AssignedOfficerId,
    Guid? SurveyorId,
    DateTime? SettlementDate,
    string? RejectionReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
);

public record ClaimStatusHistoryDto(
    Guid Id,
    Guid ClaimId,
    string OldStatus,
    string NewStatus,
    Guid? ChangedById,
    string Notes,
    DateTimeOffset ChangedAt
);



public record AssignSurveyorRequest(
    Guid SurveyorId,
    string Notes
);

public record UploadClaimDocumentRequest(
    string DocumentType,
    IFormFile File
);

public record ApproveRejectClaimRequest(
    bool IsApproved,
    decimal? ApprovedAmount,
    string Reason
);

public record SubmitSurveyReportRequest(
    decimal EstimatedRepairCost,
    DateTime SurveyDate,
    string Remarks,
    IFormFile ReportDocument
);
