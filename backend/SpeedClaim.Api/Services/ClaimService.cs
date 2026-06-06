using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SpeedClaim.Api.Context;
using SpeedClaim.Api.Dtos.Claims;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;

namespace SpeedClaim.Api.Services;

public class ClaimService : IClaimService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStorageService _storageService;
    private readonly IEmailService _emailService;

    public ClaimService(IUnitOfWork unitOfWork, IStorageService storageService, IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _storageService = storageService;
        _emailService = emailService;
    }

    public async Task<SpeedClaim.Api.Dtos.Common.PagedResponse<ClaimResponse>> GetAllClaimsAsync(Guid userId, int pageNumber, int pageSize)
    {
        var (claims, totalCount) = await _unitOfWork.Claims.GetPagedAsync(
            pageNumber,
            pageSize,
            c => c.SubmittedById == userId,
            query => query.Include(c => c.Policy).Include(c => c.Workflows)
        );

        var claimDtos = claims.Select(c => new ClaimResponse(
            c.Id,
            c.ClaimNumber,
            c.Status,
            c.ClaimedAmount,
            c.CreatedAt
        )).ToList();

        return new SpeedClaim.Api.Dtos.Common.PagedResponse<ClaimResponse>(claimDtos, pageNumber, pageSize, totalCount);
    }

    public async Task<IEnumerable<ClaimDocumentChecklistDto>> GetClaimChecklistAsync(Guid claimId)
    {
        var checklists = await _unitOfWork.ClaimDocumentChecklists.FindAsync(c => c.ClaimId == claimId);
        return checklists.Select(c => new ClaimDocumentChecklistDto
        {
            Id = c.Id,
            ClaimId = c.ClaimId,
            DocumentTypeCode = c.DocumentTypeCode,
            IsReceived = c.IsReceived
        });
    }

    public async Task<ClaimResponse> SubmitClaimAsync(Guid userId, SubmitClaimRequest request)
    {
        // 1. Validate Policy
        var policy = await _unitOfWork.Policies.GetByIdAsync(request.PolicyId);
        if (policy == null)
            throw new ArgumentException("Policy not found.");

        if (policy.UserId != userId)
            throw new UnauthorizedAccessException("You are not authorized to file a claim against this policy.");

        if (policy.Status != "ACTIVE")
            throw new ArgumentException($"Cannot file a claim against a policy with status: {policy.Status}");

        // 2. Create Claim
        var claimId = Guid.NewGuid();
        var claimNumber = $"CLM-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}";
        
        var claim = new Claim
        {
            Id = claimId,
            ClaimNumber = claimNumber,
            PolicyId = policy.Id,
            SubmittedById = userId,
            Status = "SUBMITTED",
            ClaimedAmount = request.AmountRequested,
            IncidentDescription = request.Description,
            IncidentDate = request.IncidentDate.ToUniversalTime(),
            Domain = policy.Domain, // Infer from policy
            CreatedAt = DateTime.UtcNow
        };
        
        if (policy.Domain == "HEALTH" && request.HealthDetail != null)
        {
            claim.HealthDetail = new ClaimHealthDetail
            {
                ClaimId = claimId,
                HospitalName = request.HealthDetail.HospitalName,
                Diagnosis = request.HealthDetail.Diagnosis,
                TreatingDoctor = request.HealthDetail.TreatingDoctor,
                AdmissionDate = request.HealthDetail.AdmissionDate.ToUniversalTime(),
                DischargeDate = request.HealthDetail.DischargeDate?.ToUniversalTime(),
                IsCashless = request.HealthDetail.IsCashless,
                InsuredMemberId = request.HealthDetail.InsuredMemberId
            };
        }
        else if (policy.Domain == "VEHICLE" && request.VehicleDetail != null)
        {
            claim.VehicleDetail = new ClaimVehicleDetail
            {
                ClaimId = claimId,
                AccidentLocation = request.VehicleDetail.AccidentLocation,
                FirNumber = request.VehicleDetail.FirNumber,
                RepairEstimate = request.VehicleDetail.RepairEstimate,
                IsTotalLoss = request.VehicleDetail.IsTotalLoss,
                SurveyorName = request.VehicleDetail.SurveyorName
            };
        }
        else if (policy.Domain == "LIFE" && request.LifeDetail != null)
        {
            claim.LifeDetail = new ClaimLifeDetail
            {
                ClaimId = claimId,
                CauseOfDeath = request.LifeDetail.CauseOfDeath,
                PlaceOfDeath = request.LifeDetail.PlaceOfDeath,
                DeathCertificateNumber = request.LifeDetail.DeathCertificateNumber,
                CertifyingDoctor = request.LifeDetail.CertifyingDoctor,
                ClaimantName = request.LifeDetail.ClaimantName,
                ClaimantRelation = request.LifeDetail.ClaimantRelation
            };
        }

        // Initialize document checklist based on domain (simplified version)
        var docTypes = await _unitOfWork.DocumentTypes.FindAsync(dt => dt.Domain == policy.Domain);
        foreach (var dt in docTypes)
        {
            claim.DocumentChecklists.Add(new ClaimDocumentChecklist
            {
                Id = Guid.NewGuid(),
                ClaimId = claimId,
                Domain = policy.Domain,
                DocumentTypeCode = dt.Code,
                IsReceived = false
            });
        }

        await _unitOfWork.Claims.AddAsync(claim);

        // 3. Create initial Workflow history
        var workflow = new ClaimWorkflow
        {
            Id = Guid.NewGuid(),
            ClaimId = claim.Id,
            ActorId = userId,
            FromStatus = null,
            ToStatus = "SUBMITTED",
            Remarks = "Claim submitted by customer.",
            TransitionedAt = DateTime.UtcNow
        };
        await _unitOfWork.ClaimWorkflows.AddAsync(workflow);
        await _unitOfWork.CompleteAsync();

        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user != null)
        {
            var subject = $"Claim Submitted: {claim.ClaimNumber}";
            var body = $"<h1>Claim Submitted</h1><p>Dear {user.FullName}, your claim for {policy.Domain} policy {policy.PolicyNumber} has been submitted successfully.</p>";
            await _emailService.SendEmailAsync(user.Email, user.FullName, subject, body);
        }

        // 4. Handle Attachments
        if (request.Attachments != null && request.Attachments.Any())
        {
            foreach (var file in request.Attachments)
            {
                // Upload to local storage
                var fileId = await _storageService.UploadFileAsync(file.OpenReadStream(), file.FileName, userId.ToString());

                var document = new Document
                {
                    Id = Guid.NewGuid(),
                    ClaimId = claim.Id,
                    UserId = userId,
                    Domain = policy.Domain,
                    DocumentTypeCode = "SUPPORTING_DOC", // Default for now, ideally parsed or user-selected
                    FileName = file.FileName,
                    FilePath = fileId,
                    VerificationStatus = "PENDING",
                    UploadedAt = DateTime.UtcNow
                };

                await _unitOfWork.Documents.AddAsync(document);
            }
        }

        await _unitOfWork.CompleteAsync();

        return new ClaimResponse(
            claim.Id,
            claim.ClaimNumber,
            claim.Status,
            claim.ClaimedAmount,
            claim.CreatedAt
        );
    }

    public async Task<ClaimResponse> UpdateClaimStatusAsync(Guid claimId, Guid actorId, UpdateClaimStatusRequest request)
    {
        var claim = await _unitOfWork.Claims.GetByIdAsync(claimId);
        if (claim == null)
            throw new ArgumentException("Claim not found.");

        var validStatuses = new[] { "SUBMITTED", "UNDER_REVIEW", "ESCALATED", "APPROVED", "REJECTED", "SETTLED", "CLOSED" };
        if (!validStatuses.Contains(request.Status.ToUpper()))
            throw new ArgumentException($"Invalid status. Allowed values: {string.Join(", ", validStatuses)}");

        var oldStatus = claim.Status;
        claim.Status = request.Status.ToUpper();
        
        if (request.ApprovedAmount.HasValue && request.Status.ToUpper() == "APPROVED")
        {
            claim.ApprovedAmount = request.ApprovedAmount.Value;
        }

        var workflow = new ClaimWorkflow
        {
            Id = Guid.NewGuid(),
            ClaimId = claim.Id,
            ActorId = actorId,
            FromStatus = oldStatus,
            ToStatus = claim.Status,
            Remarks = request.Remarks,
            TransitionedAt = DateTime.UtcNow
        };

        await _unitOfWork.ClaimWorkflows.AddAsync(workflow);
        await _unitOfWork.CompleteAsync();

        var user = await _unitOfWork.Users.GetByIdAsync(claim.SubmittedById);
        if (user != null)
        {
            var subject = $"Claim Status Updated: {claim.ClaimNumber}";
            var body = $"<h1>Claim Update</h1><p>Dear {user.FullName}, your claim {claim.ClaimNumber} status has changed from {oldStatus} to {claim.Status}.</p><p>Remarks: {request.Remarks}</p>";
            await _emailService.SendEmailAsync(user.Email, user.FullName, subject, body);
        }

        return new ClaimResponse(
            claim.Id,
            claim.ClaimNumber,
            claim.Status,
            claim.ClaimedAmount,
            claim.CreatedAt
        );
    }
}
