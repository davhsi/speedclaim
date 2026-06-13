using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SpeedClaim.Api.Dtos.Claims;
using SpeedClaim.Api.Dtos.Common;
using SpeedClaim.Api.Models.Enums;

namespace SpeedClaim.Api.Interfaces;

public interface IClaimService
{
    // Customer
    Task<ClaimDto> IntimateClaimAsync(Guid customerId, IntimateClaimRequest request);
    Task<string> UploadClaimDocumentAsync(Guid claimId, Guid customerId, string documentType, IFormFile file);
    Task<IEnumerable<ClaimDto>> GetMyClaimsAsync(Guid customerId, ClaimStatus? status = null, ClaimType? claimType = null);
    Task<ClaimDto> GetClaimByIdAsync(Guid claimId, Guid? customerId = null);
    Task<IEnumerable<ClaimStatusHistoryDto>> GetClaimHistoryAsync(Guid claimId, Guid? customerId = null);

    // Claims Officer
    Task<PagedResponse<ClaimDto>> GetAllClaimsAsync(int page, int pageSize, ClaimStatus? status = null, ClaimType? claimType = null);
    Task AssignClaimAsync(Guid claimId, Guid officerId);
    Task UpdateClaimStatusAsync(Guid claimId, ClaimStatus status, Guid officerId, string notes);
    Task RequestAdditionalDocumentsAsync(Guid claimId, string details, Guid officerId);
    Task ApproveOrRejectClaimAsync(Guid claimId, ApproveRejectClaimRequest request, Guid officerId);
    Task ApproveCashlessPreAuthAsync(Guid claimId, Guid officerId);
    Task AssignSurveyorAsync(Guid claimId, Guid surveyorId, Guid officerId, string notes);
    Task MarkClaimAsSettledAsync(Guid claimId, Guid officerId);

    // Surveyor
    Task<IEnumerable<ClaimDto>> GetAssignedMotorClaimsAsync(Guid surveyorId);
    Task<string> SubmitSurveyReportAsync(Guid claimId, Guid surveyorId, SubmitSurveyReportRequest request);
}
