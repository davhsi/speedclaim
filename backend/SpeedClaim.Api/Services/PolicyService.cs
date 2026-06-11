using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpeedClaim.Api.Dtos.Policies;
using SpeedClaim.Api.Exceptions;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;
using SpeedClaim.Api.Models.Enums;

namespace SpeedClaim.Api.Services;

public class PolicyService : IPolicyService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStorageService _storageService;

    public PolicyService(IUnitOfWork unitOfWork, IStorageService storageService)
    {
        _unitOfWork = unitOfWork;
        _storageService = storageService;
    }

    public async Task<IEnumerable<PolicyDto>> GetMyPoliciesAsync(Guid customerId)
    {
        var policies = await _unitOfWork.Policies.FindAsync(p => p.CustomerId == customerId);
        return policies.Select(MapToDto);
    }

    public async Task<PolicyDto> GetByIdAsync(Guid policyId, Guid? customerId = null)
    {
        var policy = await _unitOfWork.Policies.GetByIdAsync(policyId);
        if (policy == null)
            throw new NotFoundException("Policy not found.");
        if (customerId.HasValue && policy.CustomerId != customerId.Value)
            throw new ForbiddenException("Access denied to this policy.");
        return MapToDto(policy);
    }

    public async Task<IEnumerable<PolicyNomineeDto>> GetNomineesAsync(Guid policyId, Guid customerId)
    {
        var policy = await _unitOfWork.Policies.GetByIdAsync(policyId);
        if (policy == null || policy.CustomerId != customerId)
            throw new ForbiddenException("Access denied or policy not found.");

        if (!policy.ProposalId.HasValue)
            return Enumerable.Empty<PolicyNomineeDto>();

        var nominees = await _unitOfWork.Nominees.FindAsync(n => n.ProposalId == policy.ProposalId.Value);
        return nominees.Select(n => new PolicyNomineeDto(
            n.Id, n.FullName, n.Relationship, n.DateOfBirth, n.SharePercentage, n.IsMinor, n.AppointeeName));
    }

    public async Task CancelPolicyAsync(Guid policyId, Guid customerId)
    {
        var policy = await _unitOfWork.Policies.GetByIdAsync(policyId);
        if (policy == null || policy.CustomerId != customerId)
            throw new NotFoundException("Policy not found.");

        if (policy.Status == PolicyStatus.Cancelled)
            throw new ConflictException("Policy is already cancelled.");

        if (policy.Status != PolicyStatus.Active && policy.Status != PolicyStatus.Pending)
            throw new UnprocessableException("Only Active or Pending policies can be cancelled.");

        var oldStatus = policy.Status;
        policy.Status = PolicyStatus.Cancelled;
        policy.UpdatedAt = DateTimeOffset.UtcNow;

        await _unitOfWork.PolicyStatusHistories.AddAsync(new PolicyStatusHistory
        {
            Id = Guid.NewGuid(),
            PolicyId = policyId,
            OldStatus = oldStatus,
            NewStatus = PolicyStatus.Cancelled,
            Reason = "Cancelled by customer.",
            ChangedById = customerId,
            ChangedAt = DateTimeOffset.UtcNow
        });

        await _unitOfWork.CompleteAsync();
    }

    public async Task<byte[]> DownloadPolicyDocumentAsync(Guid policyId, Guid customerId)
    {
        var policy = await _unitOfWork.Policies.GetByIdAsync(policyId);
        if (policy == null || policy.CustomerId != customerId)
            throw new NotFoundException("Policy not found.");

        // Generate a text-based policy document on the fly
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("========================================");
        sb.AppendLine("            POLICY DOCUMENT             ");
        sb.AppendLine("========================================");
        sb.AppendLine($"Policy Number : {policy.PolicyNumber}");
        sb.AppendLine($"Status        : {policy.Status}");
        sb.AppendLine($"Type          : {policy.PolicyType}");
        sb.AppendLine($"Sum Assured   : {policy.SumAssured}");
        sb.AppendLine($"Premium       : {policy.PremiumAmount} ({policy.PaymentFrequency})");
        sb.AppendLine($"Valid From    : {policy.StartDate:yyyy-MM-dd}");
        sb.AppendLine($"Valid To      : {policy.EndDate:yyyy-MM-dd}");
        if (policy.IssuedAt.HasValue)
            sb.AppendLine($"Issued At     : {policy.IssuedAt.Value:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("========================================");

        return System.Text.Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task RequestEndorsementAsync(Guid policyId, Guid customerId, RequestEndorsementRequest request)
    {
        var policy = await _unitOfWork.Policies.GetByIdAsync(policyId);
        if (policy == null || policy.CustomerId != customerId)
            throw new NotFoundException("Policy not found.");

        var endorsement = new Endorsement
        {
            Id = Guid.NewGuid(),
            PolicyId = policyId,
            EndorsementType = request.EndorsementType,
            Description = request.Description,
            OldValue = request.OldValue,
            NewValue = request.NewValue,
            Status = EndorsementStatus.Requested,
            RequestedById = customerId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _unitOfWork.Endorsements.AddAsync(endorsement);
        await _unitOfWork.CompleteAsync();
    }

    public async Task<IEnumerable<PolicyDto>> GetAssignedCustomerPoliciesAsync(Guid agentId)
    {
        var policies = await _unitOfWork.Policies.FindAsync(p => p.AgentId == agentId);
        return policies.Select(MapToDto);
    }

    public async Task<IEnumerable<PolicyDto>> GetAllPoliciesAsync()
    {
        var policies = await _unitOfWork.Policies.GetAllAsync();
        return policies.Select(MapToDto);
    }

    public async Task<IEnumerable<PolicyStatusHistoryDto>> GetPolicyHistoryAsync(Guid policyId, Guid? customerId = null)
    {
        if (customerId.HasValue)
        {
            var policy = await _unitOfWork.Policies.GetByIdAsync(policyId);
            if (policy == null || policy.CustomerId != customerId.Value)
                throw new ForbiddenException("Access denied to this policy.");
        }

        var history = await _unitOfWork.PolicyStatusHistories.FindAsync(h => h.PolicyId == policyId);
        return history.Select(h => new PolicyStatusHistoryDto(
            h.Id,
            h.PolicyId,
            h.NewStatus.ToString(),
            h.Reason,
            h.ChangedById ?? Guid.Empty,
            h.ChangedAt
        ));
    }

    public async Task ApproveRejectEndorsementAsync(Guid endorsementId, bool isApproved, string reason, Guid underwriterId)
    {
        var endorsement = await _unitOfWork.Endorsements.GetByIdAsync(endorsementId);
        if (endorsement == null)
            throw new NotFoundException("Endorsement not found.");

        if (endorsement.Status != EndorsementStatus.Requested)
            throw new UnprocessableException("Endorsement is not in Requested status.");

        endorsement.Status = isApproved ? EndorsementStatus.Approved : EndorsementStatus.Rejected;
        endorsement.ReviewedById = underwriterId;
        endorsement.ReviewedAt = DateTimeOffset.UtcNow;
        endorsement.UpdatedAt = DateTimeOffset.UtcNow;

        _unitOfWork.Endorsements.Update(endorsement);
        await _unitOfWork.CompleteAsync();
    }

    private static PolicyDto MapToDto(Policy p)
    {
        return new PolicyDto(
            p.Id,
            p.PolicyNumber,
            p.CustomerId,
            p.ProductId,
            p.AgentId,
            p.Status,
            p.PaymentFrequency,
            p.PremiumAmount,
            p.SumAssured,
            "USD",
            p.StartDate,
            p.EndDate,
            p.PolicyType.ToString(),
            null,
            null,
            null
        );
    }

    public async Task UpdateNomineeAsync(Guid nomineeId, Guid customerId, UpdateNomineeRequest request)
    {
        var nominee = await _unitOfWork.Nominees.GetByIdAsync(nomineeId);
        if (nominee == null) throw new NotFoundException("Nominee not found.");

        // Verify this nominee belongs to a policy owned by this customer
        if (nominee.PolicyId.HasValue)
        {
            var policy = await _unitOfWork.Policies.GetByIdAsync(nominee.PolicyId.Value);
            if (policy == null || policy.CustomerId != customerId)
                throw new ForbiddenException("Access denied to this nominee.");
        }
        else if (nominee.ProposalId != Guid.Empty)
        {
            var proposal = await _unitOfWork.Proposals.GetByIdAsync(nominee.ProposalId);
            if (proposal == null || proposal.CustomerId != customerId)
                throw new ForbiddenException("Access denied to this nominee.");
        }

        nominee.FullName = request.FullName;
        nominee.Relationship = request.Relationship;
        nominee.DateOfBirth = request.DateOfBirth;
        nominee.SharePercentage = request.SharePercentage;
        nominee.IsMinor = request.IsMinor;
        nominee.AppointeeName = request.AppointeeName;
        nominee.UpdatedAt = DateTimeOffset.UtcNow;

        _unitOfWork.Nominees.Update(nominee);
        await _unitOfWork.CompleteAsync();
    }

    public async Task<IEnumerable<EndorsementDto>> GetPolicyEndorsementsAsync(Guid policyId, Guid customerId)
    {
        var policy = await _unitOfWork.Policies.GetByIdAsync(policyId);
        if (policy == null || policy.CustomerId != customerId)
            throw new ForbiddenException("Access denied or policy not found");

        var endorsements = await _unitOfWork.Endorsements.FindAsync(e => e.PolicyId == policyId);
        return endorsements.Select(e => new EndorsementDto(
            e.Id,
            e.PolicyId,
            e.EndorsementType.ToString(),
            e.Description,
            e.OldValue,
            e.NewValue,
            e.Status.ToString(),
            e.RequestedById,
            e.ReviewedById,
            e.ReviewedAt,
            e.CreatedAt
        ));
    }

    public async Task<IEnumerable<EndorsementDto>> GetPendingEndorsementsAsync()
    {
        var endorsements = await _unitOfWork.Endorsements.FindAsync(e => e.Status == EndorsementStatus.Requested);
        return endorsements.Select(e => new EndorsementDto(
            e.Id,
            e.PolicyId,
            e.EndorsementType.ToString(),
            e.Description,
            e.OldValue,
            e.NewValue,
            e.Status.ToString(),
            e.RequestedById,
            e.ReviewedById,
            e.ReviewedAt,
            e.CreatedAt
        ));
    }
}
