using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
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
    private readonly IEmailService _emailService;

    public ClaimService(IUnitOfWork unitOfWork, IStorageService storageService, INotificationService notifications, ILogger<ClaimService> logger, IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _storageService = storageService;
        _notifications = notifications;
        _logger = logger;
        _emailService = emailService;
    }

    // Controllers pass the authenticated User.Id (JWT sub), but Claim.CustomerId is a FK to
    // customers.id. Resolve the customer record from the user id; fall back to the supplied id
    // when it is already a customer id (keeps unit tests and internal callers working).
    public async Task WithdrawClaimAsync(Guid claimId, Guid customerId)
    {
        var claim = await _unitOfWork.Claims.GetByIdAsync(claimId);
        if (claim == null) throw new NotFoundException("Claim not found.");

        var customerRecordId = await ResolveCustomerIdAsync(customerId);
        if (claim.CustomerId != customerRecordId)
            throw new ForbiddenException("You are not the owner of this claim.");

        if (claim.Status is not (ClaimStatus.Intimated or ClaimStatus.DocumentsPending))
            throw new ConflictException($"Only claims in Intimated or DocumentsPending status can be withdrawn. Current status: {claim.Status}.");

        await UpdateClaimStatusInternalAsync(claim, ClaimStatus.Withdrawn, customerId, "Withdrawn by customer.");
    }

    private async Task<Guid> ResolveCustomerIdAsync(Guid userId)
    {
        var customer = await _unitOfWork.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
        return customer is not null && customer.Id != Guid.Empty ? customer.Id : userId;
    }

    // Surveyor-scoped endpoints receive the authenticated User.Id, but Claim.SurveyorId is a FK to
    // surveyors.id. Resolve the surveyor record from the user id; fall back to the supplied id.
    private async Task<Guid> ResolveSurveyorIdAsync(Guid userId)
    {
        var surveyor = await _unitOfWork.Surveyors.FirstOrDefaultAsync(s => s.UserId == userId);
        return surveyor is not null && surveyor.Id != Guid.Empty ? surveyor.Id : userId;
    }

    private static bool IsSurveyReportLocked(ClaimStatus status)
    {
        return status is ClaimStatus.Approved or ClaimStatus.Rejected or ClaimStatus.Settled or ClaimStatus.Withdrawn;
    }

    private static bool IsClaimActionLocked(ClaimStatus status)
    {
        return status is ClaimStatus.Approved or ClaimStatus.Rejected or ClaimStatus.Settled or ClaimStatus.Withdrawn;
    }

    private static bool CanApproveClaim(ClaimStatus status)
    {
        return status is ClaimStatus.UnderReview or ClaimStatus.PreAuthApproved;
    }

    private static bool CanRejectClaim(ClaimStatus status)
    {
        return status is ClaimStatus.Intimated or ClaimStatus.DocumentsPending or ClaimStatus.UnderReview or ClaimStatus.PreAuthApproved;
    }

    private static bool CanRequestAdditionalDocuments(ClaimStatus status)
    {
        return status is ClaimStatus.Intimated or ClaimStatus.UnderReview or ClaimStatus.PreAuthApproved;
    }

    private static bool CanAssignSurveyor(ClaimStatus status)
    {
        return status is ClaimStatus.Intimated or ClaimStatus.UnderReview;
    }

    private static bool CanCustomerUploadDocument(ClaimStatus status)
    {
        return status is ClaimStatus.Intimated or ClaimStatus.DocumentsPending;
    }

    private static bool CanTransitionClaimStatus(ClaimStatus current, ClaimStatus next)
    {
        if (current == next) return true;

        return current switch
        {
            ClaimStatus.Intimated => next is ClaimStatus.UnderReview or ClaimStatus.DocumentsPending,
            ClaimStatus.DocumentsPending => next is ClaimStatus.UnderReview,
            ClaimStatus.UnderReview => next is ClaimStatus.DocumentsPending or ClaimStatus.PreAuthRequested or ClaimStatus.Approved or ClaimStatus.Rejected,
            ClaimStatus.PreAuthRequested => next is ClaimStatus.PreAuthApproved or ClaimStatus.DocumentsPending or ClaimStatus.Rejected,
            ClaimStatus.PreAuthApproved => next is ClaimStatus.UnderReview or ClaimStatus.DocumentsPending or ClaimStatus.Approved or ClaimStatus.Rejected,
            // Approved -> Settled only happens via FinanceService (real payout or manual settle),
            // never through this generic claims-officer status endpoint — settlement implies money
            // actually moved, which is a finance responsibility, not a claims-officer one.
            _ => false
        };
    }

    private static bool IsClaimTypeAllowedForDomain(ClaimType claimType, string? domain)
    {
        return domain?.Trim().ToUpperInvariant() switch
        {
            "HEALTH" => claimType == ClaimType.Health,
            "LIFE" => claimType is ClaimType.Death or ClaimType.Maturity,
            "MOTOR" => claimType is ClaimType.Accident or ClaimType.Theft or ClaimType.NaturalDamage,
            _ => false
        };
    }

    private static string NormalizeDocumentKey(string documentType)
    {
        var key = documentType?.Trim();
        if (string.IsNullOrWhiteSpace(key))
            throw new ValidationException("Document type is required.");

        if (key.Length > 100)
            throw new ValidationException("Document type cannot exceed 100 characters.");

        if (key.Any(c => !char.IsLetterOrDigit(c) && c != '_' && c != '-'))
            throw new ValidationException("Document type can only contain letters, numbers, underscores, and hyphens.");

        return key.ToUpperInvariant();
    }

    private static void EnsureAssignedOfficer(Claim claim, Guid officerId, string action)
    {
        if (claim.AssignedOfficerId != officerId)
            throw new ForbiddenException($"Only the assigned claims officer can {action} this claim.");
    }

    private static SubmittedDocumentDto MapDocumentToDto(SubmittedDocument doc)
    {
        return new SubmittedDocumentDto(
            doc.Id,
            doc.DocumentKey,
            string.IsNullOrWhiteSpace(doc.OriginalFilename) ? doc.DocumentKey : doc.OriginalFilename,
            doc.FilePath,
            doc.UploadedAt
        );
    }

    private ClaimDto MapToDto(Claim claim, IEnumerable<SubmittedDocument>? documents = null, MotorClaimDetail? surveyDetail = null)
    {
        var user = claim.Customer?.User;
        var customerName = user != null ? $"{user.FirstName} {user.LastName}".Trim() : null;
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
            claim.UpdatedAt,
            customerName,
            claim.Policy?.PolicyNumber,
            documents?.OrderByDescending(d => d.UploadedAt).Select(MapDocumentToDto).ToList(),
            surveyDetail != null ? surveyDetail.EstimatedRepairCost : null,
            surveyDetail?.SurveyDate,
            surveyDetail?.SurveyorRemarks
        );
    }

    public async Task<ClaimDto> IntimateClaimAsync(Guid customerId, IntimateClaimRequest request)
    {
        var kyc = await _unitOfWork.KycRecords.FirstOrDefaultAsync(k => k.UserId == customerId);
        if (kyc == null || kyc.KycStatus != KycStatus.Approved)
            throw new ForbiddenException("KYC must be approved before intimating a claim.");

        var customerRecordId = await ResolveCustomerIdAsync(customerId);
        var policy = await _unitOfWork.Policies.GetByIdAsync(request.PolicyId);
        if (policy == null || policy.CustomerId != customerRecordId)
            throw new NotFoundException("Policy not found or does not belong to the customer.");

        if (policy.Status != PolicyStatus.Active)
            throw new UnprocessableException("Claim can only be intimated for an active policy.");

        var productDomain = policy.Product?.Domain;
        if (string.IsNullOrWhiteSpace(productDomain))
        {
            var product = await _unitOfWork.InsuranceProducts.GetByIdAsync(policy.ProductId);
            productDomain = product?.Domain;
        }

        if (!IsClaimTypeAllowedForDomain(request.ClaimType, productDomain))
            throw new UnprocessableException("Claim type is not valid for the selected policy.");

        var incidentDate = request.IncidentDate.Date;
        if (incidentDate < policy.StartDate.Date || incidentDate > policy.EndDate.Date)
            throw new UnprocessableException("Incident date must fall within the active policy coverage period.");

        if (policy.SumAssured > 0 && request.ClaimAmountRequested > policy.SumAssured)
            throw new UnprocessableException("Claim amount cannot exceed the policy coverage amount.");

        var claim = new Claim
        {
            Id = Guid.NewGuid(),
            ClaimNumber = $"CLM-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}",
            PolicyId = request.PolicyId,
            CustomerId = customerRecordId,
            ClaimantMemberId = request.ClaimantMemberId,
            ClaimType = request.ClaimType,
            ClaimAmountRequested = request.ClaimAmountRequested,
            IsCashless = request.IsCashless,
            Status = ClaimStatus.Intimated,
            IntimationDate = DateTime.UtcNow,
            IncidentDate = DateTime.SpecifyKind(request.IncidentDate, DateTimeKind.Utc),
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
        await _unitOfWork.AuditLogs.AddAsync(new Models.AuditLog
        {
            Id = Guid.NewGuid(), UserId = customerId, EntityType = "Claim", EntityId = claim.Id,
            Action = "ClaimIntimated",
            NewValue = JsonSerializer.Serialize(new { claimNumber = claim.ClaimNumber, policyId = claim.PolicyId }),
            CreatedAt = DateTime.UtcNow
        });
        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("Claim intimated: {ClaimNumber} for Policy {PolicyId} by Customer {CustomerId}", claim.ClaimNumber, request.PolicyId, customerId);

        var user = await _unitOfWork.Users.GetByIdAsync(customerId);
        if (user != null)
        {
            await _emailService.SendTemplatedEmailAsync("ClaimIntimated", new Dictionary<string, string>
            {
                ["firstName"]    = WebUtility.HtmlEncode(user.FirstName),
                ["claimNumber"]  = WebUtility.HtmlEncode(claim.ClaimNumber),
                ["policyNumber"] = WebUtility.HtmlEncode(policy.PolicyNumber)
            }, user.Email);
        }

        return MapToDto(claim);
    }

    public async Task<string> UploadClaimDocumentAsync(Guid claimId, Guid customerId, string documentType, IFormFile file)
    {
        var documentKey = NormalizeDocumentKey(documentType);
        var customerRecordId = await ResolveCustomerIdAsync(customerId);
        var claim = await _unitOfWork.Claims.GetByIdAsync(claimId);
        if (claim == null || claim.CustomerId != customerRecordId)
            throw new NotFoundException("Claim not found or access denied.");

        if (!CanCustomerUploadDocument(claim.Status))
            throw new ConflictException($"Documents cannot be uploaded while the claim is {claim.Status}.");

        if (file == null || file.Length == 0)
            throw new ValidationException("Invalid file.");

        using var stream = file.OpenReadStream();
        var storedPath = await _storageService.UploadFileAsync(stream, file.FileName, $"claims/{claimId}");

        var existing = await _unitOfWork.SubmittedDocuments.FindAsync(
            d => d.EntityId == claimId && d.DocumentKey == documentKey);
        foreach (var old in existing)
        {
            if (!string.IsNullOrWhiteSpace(old.FilePath))
                await _storageService.DeleteFileAsync(old.FilePath);
            _unitOfWork.SubmittedDocuments.Delete(old);
        }

        var doc = new SubmittedDocument
        {
            Id = Guid.NewGuid(),
            EntityType = EntityType.Claim,
            EntityId = claimId,
            DocumentKey = documentKey,
            OriginalFilename = file.FileName,
            StoredFilename = storedPath.Split('/').Last(),
            FilePath = storedPath,
            UploadedBy = customerId,
            UploadedAt = DateTime.UtcNow
        };

        await _unitOfWork.SubmittedDocuments.AddAsync(doc);
        
        if (claim.Status == ClaimStatus.Intimated || claim.Status == ClaimStatus.DocumentsPending)
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
        var customerRecordId = await ResolveCustomerIdAsync(customerId);
        var claims = await _unitOfWork.Claims.FindAsync(c =>
            c.CustomerId == customerRecordId &&
            (!status.HasValue || c.Status == status.Value) &&
            (!claimType.HasValue || c.ClaimType == claimType.Value));
        return claims.Select(claim => MapToDto(claim));
    }

    public async Task<ClaimDto> GetClaimByIdAsync(Guid claimId, Guid? customerId = null)
    {
        var claim = await _unitOfWork.Claims.GetByIdAsync(claimId);
        if (claim == null)
            throw new NotFoundException("Claim not found.");
        if (customerId.HasValue && claim.CustomerId != await ResolveCustomerIdAsync(customerId.Value))
            throw new ForbiddenException("Access denied to this claim.");

        var documents = await _unitOfWork.SubmittedDocuments.FindAsync(d =>
            d.EntityType == EntityType.Claim && d.EntityId == claimId);
        var surveyDetail = await _unitOfWork.MotorClaimDetails.FirstOrDefaultAsync(m => m.ClaimId == claimId);
        return MapToDto(claim, documents, surveyDetail);
    }

    public async Task<IEnumerable<ClaimStatusHistoryDto>> GetClaimHistoryAsync(Guid claimId, Guid? customerId = null)
    {
        var claim = await _unitOfWork.Claims.GetByIdAsync(claimId);
        if (claim == null)
            throw new NotFoundException("Claim not found.");
        if (customerId.HasValue && claim.CustomerId != await ResolveCustomerIdAsync(customerId.Value))
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
        return new PagedResponse<ClaimDto>(items.Select(claim => MapToDto(claim)), page, pageSize, total);
    }

    public async Task AssignClaimAsync(Guid claimId, Guid officerId)
    {
        var claim = await _unitOfWork.Claims.GetByIdAsync(claimId);
        if (claim == null) throw new NotFoundException("Claim not found.");

        if (IsClaimActionLocked(claim.Status))
            throw new ConflictException($"Claim cannot be assigned while it is {claim.Status}.");

        if (claim.AssignedOfficerId.HasValue && claim.AssignedOfficerId.Value != officerId)
            throw new ConflictException("Claim is already assigned to another officer.");

        claim.AssignedOfficerId = officerId;
        claim.UpdatedAt = DateTimeOffset.UtcNow;
        
        await UpdateClaimStatusInternalAsync(claim, ClaimStatus.UnderReview, officerId, "Claim assigned to officer.");
    }

    public async Task UpdateClaimStatusAsync(Guid claimId, ClaimStatus status, Guid officerId, string notes)
    {
        var claim = await _unitOfWork.Claims.GetByIdAsync(claimId);
        if (claim == null) throw new NotFoundException("Claim not found.");

        EnsureAssignedOfficer(claim, officerId, "update status for");

        if (!CanTransitionClaimStatus(claim.Status, status))
            throw new ConflictException($"Claim status cannot move from {claim.Status} to {status}.");

        await UpdateClaimStatusInternalAsync(claim, status, officerId, notes);
    }

    public async Task RequestAdditionalDocumentsAsync(Guid claimId, string details, Guid officerId)
    {
        var claim = await _unitOfWork.Claims.GetByIdAsync(claimId);
        if (claim == null) throw new NotFoundException("Claim not found.");

        EnsureAssignedOfficer(claim, officerId, "request documents for");

        if (!CanRequestAdditionalDocuments(claim.Status))
            throw new ConflictException($"Additional documents cannot be requested while the claim is {claim.Status}.");

        await UpdateClaimStatusInternalAsync(claim, ClaimStatus.DocumentsPending, officerId, $"Additional documents requested: {details}");
    }

    public async Task ApproveOrRejectClaimAsync(Guid claimId, ApproveRejectClaimRequest request, Guid officerId)
    {
        var claim = await _unitOfWork.Claims.GetByIdAsync(claimId);
        if (claim == null) throw new NotFoundException("Claim not found.");

        EnsureAssignedOfficer(claim, officerId, "approve or reject");

        var newStatus = request.IsApproved ? ClaimStatus.Approved : ClaimStatus.Rejected;
        
        if (request.IsApproved)
        {
            if (!CanApproveClaim(claim.Status))
                throw new ConflictException($"Claim cannot be approved while it is {claim.Status}.");
            if (!request.ApprovedAmount.HasValue || request.ApprovedAmount.Value <= 0)
                throw new ValidationException("Approved amount must be specified and greater than zero.");
            if (request.ApprovedAmount.Value > claim.ClaimAmountRequested)
                throw new ValidationException("Approved amount cannot exceed the requested claim amount.");
            claim.ClaimAmountApproved = request.ApprovedAmount.Value;
        }
        else
        {
            if (!CanRejectClaim(claim.Status))
                throw new ConflictException($"Claim cannot be rejected while it is {claim.Status}.");
            claim.RejectionReason = request.Reason;
        }

        await UpdateClaimStatusInternalAsync(claim, newStatus, officerId, request.Reason);
    }

    public async Task ApproveCashlessPreAuthAsync(Guid claimId, Guid officerId)
    {
        var claim = await _unitOfWork.Claims.GetByIdAsync(claimId);
        if (claim == null) throw new NotFoundException("Claim not found.");

        EnsureAssignedOfficer(claim, officerId, "approve pre-authorisation for");
        
        if (!claim.IsCashless) throw new UnprocessableException("Claim is not a cashless claim.");
        if (claim.Status != ClaimStatus.PreAuthRequested)
            throw new ConflictException("Cashless pre-authorisation can only be approved while the claim is awaiting pre-auth.");

        await UpdateClaimStatusInternalAsync(claim, ClaimStatus.PreAuthApproved, officerId, "Cashless Pre-Auth Approved.");
    }

    public async Task AssignSurveyorAsync(Guid claimId, Guid surveyorId, Guid officerId, string notes)
    {
        var claim = await _unitOfWork.Claims.GetByIdAsync(claimId);
        if (claim == null) throw new NotFoundException("Claim not found.");

        EnsureAssignedOfficer(claim, officerId, "assign a surveyor to");

        if (claim.ClaimType != ClaimType.Accident && claim.ClaimType != ClaimType.Theft && claim.ClaimType != ClaimType.NaturalDamage)
            throw new UnprocessableException("Surveyor can only be assigned to Motor or Property related claims.");

        if (!CanAssignSurveyor(claim.Status))
            throw new ConflictException($"Surveyor cannot be assigned while the claim is {claim.Status}.");

        claim.SurveyorId = surveyorId;
        await UpdateClaimStatusInternalAsync(claim, ClaimStatus.UnderReview, officerId, $"Surveyor assigned. Notes: {notes}");
    }

    public async Task<IEnumerable<ClaimDto>> GetAssignedMotorClaimsAsync(Guid surveyorId)
    {
        var surveyorRecordId = await ResolveSurveyorIdAsync(surveyorId);
        var claims = await _unitOfWork.Claims.FindAsync(c =>
            c.SurveyorId == surveyorRecordId &&
            (c.ClaimType == ClaimType.Accident || c.ClaimType == ClaimType.Theft || c.ClaimType == ClaimType.NaturalDamage));
        return claims.Select(claim => MapToDto(claim));
    }

    public async Task<string> SubmitSurveyReportAsync(Guid claimId, Guid surveyorId, SubmitSurveyReportRequest request)
    {
        var surveyorRecordId = await ResolveSurveyorIdAsync(surveyorId);
        var claim = await _unitOfWork.Claims.GetByIdAsync(claimId);
        if (claim == null || claim.SurveyorId != surveyorRecordId)
            throw new NotFoundException("Claim not found or surveyor not assigned to this claim.");

        if (IsSurveyReportLocked(claim.Status))
            throw new ConflictException($"Survey report cannot be submitted for a {claim.Status} claim.");

        var existingReport = await _unitOfWork.SubmittedDocuments.FirstOrDefaultAsync(d =>
            d.EntityType == EntityType.Claim &&
            d.EntityId == claimId &&
            d.DocumentKey == "SurveyorReport");

        if (existingReport != null)
            throw new ConflictException("Survey report has already been submitted for this claim.");

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

        if (request.Photos != null)
        {
            var photoIndex = 1;
            foreach (var photo in request.Photos.Where(p => p != null && p.Length > 0).Take(10))
            {
                using var photoStream = photo.OpenReadStream();
                var photoPath = await _storageService.UploadFileAsync(photoStream, photo.FileName, $"claims/{claimId}/survey/photos");
                await _unitOfWork.SubmittedDocuments.AddAsync(new SubmittedDocument
                {
                    Id = Guid.NewGuid(),
                    EntityType = EntityType.Claim,
                    EntityId = claimId,
                    DocumentKey = $"SurveyPhoto{photoIndex}",
                    OriginalFilename = photo.FileName,
                    StoredFilename = photoPath.Split('/').Last(),
                    FilePath = photoPath,
                    UploadedBy = surveyorId,
                    UploadedAt = DateTime.UtcNow
                });
                photoIndex++;
            }
        }
        
        // MotorClaimDetail is the persistent home for the survey assessment — no other flow
        // creates this row, so upsert it here or the assessed cost is lost entirely.
        var motorDetail = await _unitOfWork.MotorClaimDetails.FirstOrDefaultAsync(m => m.ClaimId == claimId);
        if (motorDetail == null)
        {
            motorDetail = new MotorClaimDetail { Id = Guid.NewGuid(), ClaimId = claimId };
            await _unitOfWork.MotorClaimDetails.AddAsync(motorDetail);
        }
        motorDetail.EstimatedRepairCost = request.EstimatedRepairCost;
        motorDetail.SurveyDate = request.SurveyDate.ToUniversalTime();
        motorDetail.SurveyorRemarks = request.Remarks;

        await UpdateClaimStatusInternalAsync(claim, ClaimStatus.UnderReview, surveyorId, $"Surveyor report uploaded. Estimated repair cost: INR {request.EstimatedRepairCost:N0}. Remarks: {request.Remarks}");
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
            OldValue = JsonSerializer.Serialize(history.OldStatus.ToString()), NewValue = JsonSerializer.Serialize(newStatus.ToString()),
            CreatedAt = DateTime.UtcNow
        });
        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("Claim {ClaimId} status changed to {NewStatus} by {ChangedById}", claim.Id, newStatus, changedById);

        // Notify and email the customer about claim status change
        var customer = await _unitOfWork.Customers.GetByIdAsync(claim.CustomerId);
        if (customer != null)
        {
            await _notifications.CreateAsync(
                customer.UserId,
                "Claim Status Updated",
                $"Your claim {claim.ClaimNumber} status has changed to: {newStatus}.",
                "claim",
                $"/claims/{claim.Id}");

            var emailTemplateKey = newStatus switch
            {
                ClaimStatus.DocumentsPending => "ClaimDocumentsPending",
                ClaimStatus.UnderReview      => "ClaimUnderReview",
                ClaimStatus.PreAuthRequested => "ClaimPreAuthRequested",
                ClaimStatus.PreAuthApproved  => "ClaimPreAuthApproved",
                ClaimStatus.Approved         => "ClaimApproved",
                ClaimStatus.Rejected         => "ClaimRejected",
                ClaimStatus.Settled          => "ClaimSettled",
                ClaimStatus.Withdrawn        => "ClaimWithdrawn",
                _                            => null
            };

            if (emailTemplateKey != null)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(customer.UserId);
                if (user != null)
                {
                    var vars = new Dictionary<string, string>
                    {
                        ["firstName"]   = WebUtility.HtmlEncode(user.FirstName),
                        ["claimNumber"] = WebUtility.HtmlEncode(claim.ClaimNumber)
                    };
                    if (newStatus == ClaimStatus.Rejected && !string.IsNullOrWhiteSpace(notes))
                        vars["rejectionReason"] = WebUtility.HtmlEncode(notes);
                    if (newStatus == ClaimStatus.Settled)
                        vars["payoutAmount"] = claim.ClaimAmountApproved?.ToString("N0") ?? "0";
                    await _emailService.SendTemplatedEmailAsync(emailTemplateKey, vars, user.Email);
                }
            }
        }
    }
}
