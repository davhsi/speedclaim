using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SpeedClaim.Api.Dtos.Claims;
using SpeedClaim.Api.Dtos.Common;
using SpeedClaim.Api.Exceptions;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;
using SpeedClaim.Api.Models.Enums;

namespace SpeedClaim.Api.Services;

public class ClaimService : IClaimService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStorageService _storageService;
    private readonly INotificationService _notifications;
    private readonly ILogger<ClaimService> _logger;

    public ClaimService(IUnitOfWork unitOfWork, IStorageService storageService, INotificationService notifications, ILogger<ClaimService> logger)
    {
        _unitOfWork = unitOfWork;
        _storageService = storageService;
        _notifications = notifications;
        _logger = logger;
    }

    private ClaimDto MapToDto(Claim claim)
    {
        return new ClaimDto(
            claim.Id,
            claim.ClaimNumber,
            claim.PolicyId,
            claim.CustomerId,
            claim.ClaimantMemberId,
            claim.ClaimType.ToString(),
            claim.ClaimAmountRequested,
            claim.ClaimAmountApproved,
            claim.IsCashless,
            claim.Status.ToString(),
            claim.IntimationDate,
            claim.IncidentDate,
            claim.IncidentDescription,
            claim.AssignedOfficerId,
            claim.SurveyorId,
            claim.SettlementDate,
            claim.RejectionReason,
            claim.CreatedAt,
            claim.UpdatedAt
        );
    }

    public async Task<ClaimDto> IntimateClaimAsync(Guid customerId, IntimateClaimRequest request)
    {
        var policy = await _unitOfWork.Policies.GetByIdAsync(request.PolicyId);
        if (policy == null || policy.CustomerId != customerId)
            throw new NotFoundException("Policy not found or does not belong to the customer.");

        if (policy.Status != PolicyStatus.Active)
            throw new UnprocessableException("Claim can only be intimated for an active policy.");

        var claim = new Claim
        {
            Id = Guid.NewGuid(),
            ClaimNumber = $"CLM-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}",
            PolicyId = request.PolicyId,
            CustomerId = customerId,
            ClaimantMemberId = request.ClaimantMemberId,
            ClaimType = request.ClaimType,
            ClaimAmountRequested = request.ClaimAmountRequested,
            IsCashless = request.IsCashless,
            Status = ClaimStatus.Intimated,
            IntimationDate = DateTime.UtcNow,
            IncidentDate = request.IncidentDate,
            IncidentDescription = request.IncidentDescription,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var history = new ClaimStatusHistory
        {
            Id = Guid.NewGuid(),
            ClaimId = claim.Id,
            OldStatus = ClaimStatus.Intimated,
            NewStatus = ClaimStatus.Intimated,
            ChangedById = customerId,
            Notes = "Claim intimated by customer.",
            ChangedAt = DateTimeOffset.UtcNow
        };

        await _unitOfWork.Claims.AddAsync(claim);
        await _unitOfWork.ClaimStatusHistories.AddAsync(history);
        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("Claim intimated: {ClaimNumber} for Policy {PolicyId} by Customer {CustomerId}", claim.ClaimNumber, request.PolicyId, customerId);
        return MapToDto(claim);
    }

    public async Task<string> UploadClaimDocumentAsync(Guid claimId, Guid customerId, string documentType, IFormFile file)
    {
        var claim = await _unitOfWork.Claims.GetByIdAsync(claimId);
        if (claim == null || claim.CustomerId != customerId)
            throw new NotFoundException("Claim not found or access denied.");

        if (file == null || file.Length == 0)
            throw new ValidationException("Invalid file.");

        var existing = await _unitOfWork.SubmittedDocuments.FindAsync(
            d => d.EntityId == claimId && d.DocumentKey == documentType);
        foreach (var old in existing)
            _unitOfWork.SubmittedDocuments.Delete(old);

        var doc = new SubmittedDocument
        {
            Id = Guid.NewGuid(),
            EntityType = EntityType.Claim,
            EntityId = claimId,
            DocumentKey = documentType,
            OriginalFilename = file.FileName,
            StoredFilename = $"{Guid.NewGuid()}_{file.FileName}",
            FilePath = $"/uploads/claims/{claimId}/",
            UploadedBy = customerId,
            UploadedAt = DateTime.UtcNow
        };

        await _unitOfWork.SubmittedDocuments.AddAsync(doc);
        
        if (claim.Status == ClaimStatus.Intimated)
        {
            await UpdateClaimStatusInternalAsync(claim, ClaimStatus.UnderReview, customerId, "Documents uploaded, moving to review.");
        }
        else
        {
            await _unitOfWork.CompleteAsync();
        }

        return doc.FilePath;
    }

    public async Task<IEnumerable<ClaimDto>> GetMyClaimsAsync(Guid customerId, ClaimStatus? status = null, ClaimType? claimType = null)
    {
        var claims = await _unitOfWork.Claims.FindAsync(c =>
            c.CustomerId == customerId &&
            (!status.HasValue || c.Status == status.Value) &&
            (!claimType.HasValue || c.ClaimType == claimType.Value));
        return claims.Select(MapToDto);
    }

    public async Task<ClaimDto> GetClaimByIdAsync(Guid claimId, Guid? customerId = null)
    {
        var claim = await _unitOfWork.Claims.GetByIdAsync(claimId);
        if (claim == null)
            throw new NotFoundException("Claim not found.");
        if (customerId.HasValue && claim.CustomerId != customerId.Value)
            throw new ForbiddenException("Access denied to this claim.");
        return MapToDto(claim);
    }

    public async Task<IEnumerable<ClaimStatusHistoryDto>> GetClaimHistoryAsync(Guid claimId, Guid? customerId = null)
    {
        var claim = await _unitOfWork.Claims.GetByIdAsync(claimId);
        if (claim == null || (customerId.HasValue && claim.CustomerId != customerId.Value))
            throw new NotFoundException("Claim not found.");

        var history = await _unitOfWork.ClaimStatusHistories.FindAsync(h => h.ClaimId == claimId);
        return history
            .OrderBy(h => h.ChangedAt)
            .Select(h => new ClaimStatusHistoryDto(
                h.Id,
                h.ClaimId,
                h.OldStatus.ToString(),
                h.NewStatus.ToString(),
                h.ChangedById,
                h.Notes,
                h.ChangedAt));
    }

    public async Task<PagedResponse<ClaimDto>> GetAllClaimsAsync(int page, int pageSize, ClaimStatus? status = null, ClaimType? claimType = null)
    {
        var (items, total) = await _unitOfWork.Claims.GetPagedAsync(page, pageSize, c =>
            (!status.HasValue || c.Status == status.Value) &&
            (!claimType.HasValue || c.ClaimType == claimType.Value));
        return new PagedResponse<ClaimDto>(items.Select(MapToDto), page, pageSize, total);
    }

    public async Task AssignClaimAsync(Guid claimId, Guid officerId)
    {
        var claim = await _unitOfWork.Claims.GetByIdAsync(claimId);
        if (claim == null) throw new NotFoundException("Claim not found.");

        claim.AssignedOfficerId = officerId;
        claim.UpdatedAt = DateTimeOffset.UtcNow;
        
        await UpdateClaimStatusInternalAsync(claim, ClaimStatus.UnderReview, officerId, "Claim assigned to officer.");
    }

    public async Task UpdateClaimStatusAsync(Guid claimId, ClaimStatus status, Guid officerId, string notes)
    {
        var claim = await _unitOfWork.Claims.GetByIdAsync(claimId);
        if (claim == null) throw new NotFoundException("Claim not found.");

        await UpdateClaimStatusInternalAsync(claim, status, officerId, notes);
    }

    public async Task RequestAdditionalDocumentsAsync(Guid claimId, string details, Guid officerId)
    {
        var claim = await _unitOfWork.Claims.GetByIdAsync(claimId);
        if (claim == null) throw new NotFoundException("Claim not found.");

        await UpdateClaimStatusInternalAsync(claim, ClaimStatus.DocumentsPending, officerId, $"Additional documents requested: {details}");
    }

    public async Task ApproveOrRejectClaimAsync(Guid claimId, ApproveRejectClaimRequest request, Guid officerId)
    {
        var claim = await _unitOfWork.Claims.GetByIdAsync(claimId);
        if (claim == null) throw new NotFoundException("Claim not found.");

        var newStatus = request.IsApproved ? ClaimStatus.Approved : ClaimStatus.Rejected;
        
        if (request.IsApproved)
        {
            if (!request.ApprovedAmount.HasValue || request.ApprovedAmount.Value <= 0)
                throw new ValidationException("Approved amount must be specified and greater than zero.");
            claim.ClaimAmountApproved = request.ApprovedAmount.Value;
        }
        else
        {
            claim.RejectionReason = request.Reason;
        }

        await UpdateClaimStatusInternalAsync(claim, newStatus, officerId, request.Reason);
    }

    public async Task ApproveCashlessPreAuthAsync(Guid claimId, Guid officerId)
    {
        var claim = await _unitOfWork.Claims.GetByIdAsync(claimId);
        if (claim == null) throw new NotFoundException("Claim not found.");
        
        if (!claim.IsCashless) throw new UnprocessableException("Claim is not a cashless claim.");

        await UpdateClaimStatusInternalAsync(claim, ClaimStatus.PreAuthApproved, officerId, "Cashless Pre-Auth Approved.");
    }

    public async Task AssignSurveyorAsync(Guid claimId, Guid surveyorId, Guid officerId, string notes)
    {
        var claim = await _unitOfWork.Claims.GetByIdAsync(claimId);
        if (claim == null) throw new NotFoundException("Claim not found.");

        if (claim.ClaimType != ClaimType.Accident && claim.ClaimType != ClaimType.Theft && claim.ClaimType != ClaimType.NaturalDamage)
            throw new UnprocessableException("Surveyor can only be assigned to Motor or Property related claims.");

        claim.SurveyorId = surveyorId;
        await UpdateClaimStatusInternalAsync(claim, ClaimStatus.UnderReview, officerId, $"Surveyor assigned. Notes: {notes}");
    }

    public async Task MarkClaimAsSettledAsync(Guid claimId, Guid officerId)
    {
        var claim = await _unitOfWork.Claims.GetByIdAsync(claimId);
        if (claim == null) throw new NotFoundException("Claim not found.");
        
        if (claim.Status != ClaimStatus.Approved)
            throw new UnprocessableException("Claim must be approved before settlement.");

        claim.SettlementDate = DateTime.UtcNow;
        await UpdateClaimStatusInternalAsync(claim, ClaimStatus.Settled, officerId, "Claim financially settled.");
    }

    public async Task<IEnumerable<ClaimDto>> GetAssignedMotorClaimsAsync(Guid surveyorId)
    {
        var claims = await _unitOfWork.Claims.FindAsync(c => c.SurveyorId == surveyorId);
        return claims.Select(MapToDto);
    }

    public async Task<string> SubmitSurveyReportAsync(Guid claimId, Guid surveyorId, SubmitSurveyReportRequest request)
    {
        var claim = await _unitOfWork.Claims.GetByIdAsync(claimId);
        if (claim == null || claim.SurveyorId != surveyorId)
            throw new NotFoundException("Claim not found or surveyor not assigned to this claim.");

        if (request.ReportDocument == null || request.ReportDocument.Length == 0)
            throw new ValidationException("Invalid report file.");

        // Upload using storage service
        using var stream = request.ReportDocument.OpenReadStream();
        var storedPath = await _storageService.UploadFileAsync(stream, request.ReportDocument.FileName, $"claims/{claimId}/survey");

        var doc = new SubmittedDocument
        {
            Id = Guid.NewGuid(),
            EntityType = EntityType.Claim,
            EntityId = claimId,
            DocumentKey = "SurveyorReport",
            OriginalFilename = request.ReportDocument.FileName,
            StoredFilename = $"survey_{Guid.NewGuid()}_{request.ReportDocument.FileName}",
            FilePath = storedPath, 
            UploadedBy = surveyorId,
            UploadedAt = DateTime.UtcNow
        };

        await _unitOfWork.SubmittedDocuments.AddAsync(doc);
        
        // Update MotorClaimDetail if present
        var motorDetail = await _unitOfWork.MotorClaimDetails.FirstOrDefaultAsync(m => m.ClaimId == claimId);
        if (motorDetail != null)
        {
            motorDetail.EstimatedRepairCost = request.EstimatedRepairCost;
            motorDetail.SurveyDate = request.SurveyDate.ToUniversalTime();
            motorDetail.SurveyorRemarks = request.Remarks;
        }

        await UpdateClaimStatusInternalAsync(claim, ClaimStatus.UnderReview, surveyorId, $"Surveyor report uploaded. Remarks: {request.Remarks}");
        return doc.FilePath;
    }

    private async Task UpdateClaimStatusInternalAsync(Claim claim, ClaimStatus newStatus, Guid changedById, string notes)
    {
        var history = new ClaimStatusHistory
        {
            Id = Guid.NewGuid(),
            ClaimId = claim.Id,
            OldStatus = claim.Status,
            NewStatus = newStatus,
            ChangedById = changedById,
            Notes = notes,
            ChangedAt = DateTimeOffset.UtcNow
        };

        claim.Status = newStatus;
        claim.UpdatedAt = DateTimeOffset.UtcNow;

        _unitOfWork.Claims.Update(claim);
        await _unitOfWork.ClaimStatusHistories.AddAsync(history);
        await _unitOfWork.AuditLogs.AddAsync(new Models.AuditLog
        {
            Id = Guid.NewGuid(), UserId = changedById, EntityType = "Claim", EntityId = claim.Id,
            Action = "ClaimStatusChanged",
            OldValue = history.OldStatus.ToString(), NewValue = newStatus.ToString(),
            CreatedAt = DateTime.UtcNow
        });
        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("Claim {ClaimId} status changed to {NewStatus} by {ChangedById}", claim.Id, newStatus, changedById);

        // Notify the customer about claim status change
        var customer = await _unitOfWork.Customers.GetByIdAsync(claim.CustomerId);
        if (customer != null)
        {
            await _notifications.CreateAsync(
                customer.UserId,
                "Claim Status Updated",
                $"Your claim {claim.ClaimNumber} status has changed to: {newStatus}.",
                "claim");
        }
    }
}
