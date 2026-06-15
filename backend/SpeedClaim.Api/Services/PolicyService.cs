using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SpeedClaim.Api.Dtos.Common;
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

    // Controllers pass the authenticated User.Id (JWT sub), but Policy.CustomerId / Proposal.CustomerId
    // are FKs to customers.id. Resolve the customer record from the user id; fall back to the supplied
    // id when it is already a customer id (keeps unit tests and internal callers working).
    private async Task<Guid> ResolveCustomerIdAsync(Guid userId)
    {
        var customer = await _unitOfWork.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
        return customer is not null && customer.Id != Guid.Empty ? customer.Id : userId;
    }

    public async Task<IEnumerable<PolicyDto>> GetMyPoliciesAsync(Guid customerId, PolicyStatus? status = null, PolicyType? policyType = null)
    {
        var customerRecordId = await ResolveCustomerIdAsync(customerId);
        var policies = await _unitOfWork.Policies.FindAsync(p =>
            p.CustomerId == customerRecordId &&
            (!status.HasValue || p.Status == status.Value) &&
            (!policyType.HasValue || p.PolicyType == policyType.Value));
        return policies.Select(MapToDto);
    }

    public async Task<PolicyDto> GetByIdAsync(Guid policyId, Guid? customerId = null)
    {
        var policy = await _unitOfWork.Policies.GetByIdAsync(policyId);
        if (policy == null)
            throw new NotFoundException("Policy not found.");
        if (customerId.HasValue && policy.CustomerId != await ResolveCustomerIdAsync(customerId.Value))
            throw new ForbiddenException("Access denied to this policy.");
        return MapToDto(policy);
    }

    public async Task<IEnumerable<PolicyNomineeDto>> GetNomineesAsync(Guid policyId, Guid customerId)
    {
        var customerRecordId = await ResolveCustomerIdAsync(customerId);
        var policy = await _unitOfWork.Policies.GetByIdAsync(policyId);
        if (policy == null || policy.CustomerId != customerRecordId)
            throw new ForbiddenException("Access denied or policy not found.");

        if (!policy.ProposalId.HasValue)
            return Enumerable.Empty<PolicyNomineeDto>();

        var nominees = await _unitOfWork.Nominees.FindAsync(n => n.ProposalId == policy.ProposalId.Value);
        return nominees.Select(n => new PolicyNomineeDto(
            n.Id, n.FullName, n.Relationship, n.DateOfBirth, n.SharePercentage, n.IsMinor, n.AppointeeName));
    }

    public async Task CancelPolicyAsync(Guid policyId, Guid customerId)
    {
        var customerRecordId = await ResolveCustomerIdAsync(customerId);
        var policy = await _unitOfWork.Policies.GetByIdAsync(policyId);
        if (policy == null || policy.CustomerId != customerRecordId)
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
        var customerRecordId = await ResolveCustomerIdAsync(customerId);
        var policy = await _unitOfWork.Policies.GetByIdAsync(policyId);
        if (policy == null || policy.CustomerId != customerRecordId)
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
        var customerRecordId = await ResolveCustomerIdAsync(customerId);
        var policy = await _unitOfWork.Policies.GetByIdAsync(policyId);
        if (policy == null || policy.CustomerId != customerRecordId)
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

    public async Task<IEnumerable<PolicyDto>> GetAssignedCustomerPoliciesAsync(Guid agentUserId)
    {
        var agent = await _unitOfWork.Agents.FirstOrDefaultAsync(a => a.UserId == agentUserId);
        if (agent == null) return Enumerable.Empty<PolicyDto>();
        var policies = await _unitOfWork.Policies.FindAsync(p => p.AgentId == agent.Id);
        return policies.Select(MapToDto);
    }

    public async Task<PagedResponse<PolicyDto>> GetAllPoliciesAsync(int page, int pageSize, PolicyStatus? status = null, PolicyType? policyType = null)
    {
        var (items, total) = await _unitOfWork.Policies.GetPagedAsync(page, pageSize, p =>
            (!status.HasValue || p.Status == status.Value) &&
            (!policyType.HasValue || p.PolicyType == policyType.Value));
        return new PagedResponse<PolicyDto>(items.Select(MapToDto), page, pageSize, total);
    }

    public async Task<IEnumerable<PolicyStatusHistoryDto>> GetPolicyHistoryAsync(Guid policyId, Guid? customerId = null)
    {
        if (customerId.HasValue)
        {
            var policy = await _unitOfWork.Policies.GetByIdAsync(policyId);
            if (policy == null || policy.CustomerId != await ResolveCustomerIdAsync(customerId.Value))
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
        await _unitOfWork.AuditLogs.AddAsync(new Models.AuditLog
        {
            Id = Guid.NewGuid(), UserId = underwriterId, EntityType = "Endorsement", EntityId = endorsementId,
            Action = isApproved ? "EndorsementApproved" : "EndorsementRejected",
            NewValue = JsonSerializer.Serialize(reason), CreatedAt = DateTime.UtcNow
        });
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

        var customerRecordId = await ResolveCustomerIdAsync(customerId);

        // Verify this nominee belongs to a policy owned by this customer
        if (nominee.PolicyId.HasValue)
        {
            var policy = await _unitOfWork.Policies.GetByIdAsync(nominee.PolicyId.Value);
            if (policy == null || policy.CustomerId != customerRecordId)
                throw new ForbiddenException("Access denied to this nominee.");
        }
        else if (nominee.ProposalId != Guid.Empty)
        {
            var proposal = await _unitOfWork.Proposals.GetByIdAsync(nominee.ProposalId);
            if (proposal == null || proposal.CustomerId != customerRecordId)
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
        var customerRecordId = await ResolveCustomerIdAsync(customerId);
        var policy = await _unitOfWork.Policies.GetByIdAsync(policyId);
        if (policy == null || policy.CustomerId != customerRecordId)
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

    public async Task<PagedResponse<EndorsementDto>> GetPendingEndorsementsAsync(int page, int pageSize)
    {
        var (items, total) = await _unitOfWork.Endorsements.GetPagedAsync(page, pageSize, e => e.Status == EndorsementStatus.Requested);
        var dtos = items.Select(e => new EndorsementDto(
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
        return new PagedResponse<EndorsementDto>(dtos, page, pageSize, total);
    }
}
