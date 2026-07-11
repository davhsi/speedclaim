using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
    private readonly IEmailService _emailService;

    public PolicyService(IUnitOfWork unitOfWork, IStorageService storageService, IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _storageService = storageService;
        _emailService = emailService;
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
        await _unitOfWork.AuditLogs.AddAsync(new Models.AuditLog
        {
            Id = Guid.NewGuid(), UserId = customerId, EntityType = "Policy", EntityId = policyId,
            Action = "PolicyCancelled",
            NewValue = JsonSerializer.Serialize(new { policyNumber = policy.PolicyNumber }),
            CreatedAt = DateTime.UtcNow
        });

        await _unitOfWork.CompleteAsync();

        var user = await _unitOfWork.Users.GetByIdAsync(customerId);
        if (user != null)
        {
            await _emailService.SendTemplatedEmailAsync("PolicyCancelled", new Dictionary<string, string>
            {
                ["firstName"]    = WebUtility.HtmlEncode(user.FirstName),
                ["policyNumber"] = WebUtility.HtmlEncode(policy.PolicyNumber)
            }, user.Email);
        }
    }

    public async Task<byte[]> DownloadPolicyDocumentAsync(Guid policyId, Guid customerId)
    {
        var customerRecordId = await ResolveCustomerIdAsync(customerId);
        var policy = await _unitOfWork.Policies.GetByIdAsync(policyId);
        if (policy == null || policy.CustomerId != customerRecordId)
            throw new NotFoundException("Policy not found.");

        var customer = await _unitOfWork.Customers.GetByIdAsync(policy.CustomerId);
        User? user = null;
        if (customer != null)
            user = await _unitOfWork.Users.GetByIdAsync(customer.UserId);

        var productRepo = _unitOfWork.InsuranceProducts;
        var product = productRepo == null ? null : await productRepo.GetByIdAsync(policy.ProductId);
        var customerName = user == null ? "Policyholder" : $"{user.FirstName} {user.LastName}".Trim();
        var productName = product?.ProductName ?? policy.PolicyType.ToString();

        return PolicyDocumentGenerator.GenerateCertificatePdf(policy, customerName, productName);
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
        await _unitOfWork.AuditLogs.AddAsync(new Models.AuditLog
        {
            Id = Guid.NewGuid(), UserId = customerId, EntityType = "Endorsement", EntityId = endorsement.Id,
            Action = "EndorsementRequested",
            NewValue = JsonSerializer.Serialize(new { type = request.EndorsementType.ToString(), policyId }),
            CreatedAt = DateTime.UtcNow
        });
        await _unitOfWork.CompleteAsync();

        var endorsementUser = await _unitOfWork.Users.GetByIdAsync(customerId);
        if (endorsementUser != null && policy != null)
            await _emailService.SendTemplatedEmailAsync("EndorsementRequested", new Dictionary<string, string>
            {
                ["firstName"]       = WebUtility.HtmlEncode(endorsementUser.FirstName),
                ["policyNumber"]    = WebUtility.HtmlEncode(policy.PolicyNumber ?? string.Empty),
                ["endorsementType"] = WebUtility.HtmlEncode(request.EndorsementType.ToString())
            }, endorsementUser.Email);
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

    // Approval must actually apply the requested change where the plain-text NewValue maps to a
    // structured field: SumAssuredChange → Policy.SumAssured, ContactUpdate → User.Phone. The
    // free-text types (NomineeChange, AddressChange, VehicleCorrection, Other) cannot be parsed
    // reliably, so their approval is an authorization record only — the customer applies those
    // through their own profile/nominee edit endpoints, which are not KYC-locked.
    private async Task ApplyEndorsementChangeAsync(Endorsement endorsement)
    {
        switch (endorsement.EndorsementType)
        {
            case EndorsementType.SumAssuredChange:
            {
                var raw = (endorsement.NewValue ?? string.Empty).Replace("₹", "").Replace(",", "").Trim();
                if (!decimal.TryParse(raw, out var newSum) || newSum <= 0)
                    throw new UnprocessableException("The requested sum assured is not a valid amount — reject this endorsement instead.");

                var policy = await _unitOfWork.Policies.GetByIdAsync(endorsement.PolicyId);
                if (policy == null) throw new NotFoundException("Policy not found.");

                endorsement.OldValue = policy.SumAssured.ToString("0.##");
                policy.SumAssured = newSum;
                policy.UpdatedAt = DateTimeOffset.UtcNow;
                _unitOfWork.Policies.Update(policy);
                break;
            }
            case EndorsementType.ContactUpdate:
            {
                var newPhone = (endorsement.NewValue ?? string.Empty).Replace(" ", "").Trim();
                if (!System.Text.RegularExpressions.Regex.IsMatch(newPhone, @"^\+?\d{10,14}$"))
                    throw new UnprocessableException("The requested contact number is not a valid phone number — reject this endorsement instead.");

                var policy = await _unitOfWork.Policies.GetByIdAsync(endorsement.PolicyId);
                if (policy == null) throw new NotFoundException("Policy not found.");
                var customer = await _unitOfWork.Customers.GetByIdAsync(policy.CustomerId);
                var user = customer != null ? await _unitOfWork.Users.GetByIdAsync(customer.UserId) : null;
                if (user == null) throw new NotFoundException("Customer account not found for this policy.");

                endorsement.OldValue = user.Phone;
                user.Phone = newPhone;
                user.UpdatedAt = DateTimeOffset.UtcNow;
                _unitOfWork.Users.Update(user);
                break;
            }
        }
    }

    public async Task ApproveRejectEndorsementAsync(Guid endorsementId, bool isApproved, string reason, Guid underwriterId)
    {
        var endorsement = await _unitOfWork.Endorsements.GetByIdAsync(endorsementId);
        if (endorsement == null)
            throw new NotFoundException("Endorsement not found.");

        if (endorsement.Status != EndorsementStatus.Requested)
            throw new UnprocessableException("Endorsement is not in Requested status.");

        if (isApproved)
            await ApplyEndorsementChangeAsync(endorsement);

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

        var policy = await _unitOfWork.Policies.GetByIdAsync(endorsement.PolicyId);
        if (policy != null)
        {
            var customer = await _unitOfWork.Customers.GetByIdAsync(policy.CustomerId);
            if (customer != null)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(customer.UserId);
                if (user != null)
                {
                    var templateKey = isApproved ? "EndorsementApproved" : "EndorsementRejected";
                    var vars = new Dictionary<string, string>
                    {
                        ["firstName"]       = WebUtility.HtmlEncode(user.FirstName),
                        ["policyNumber"]    = WebUtility.HtmlEncode(policy.PolicyNumber),
                        ["endorsementType"] = WebUtility.HtmlEncode(endorsement.EndorsementType.ToString())
                    };
                    if (!isApproved && !string.IsNullOrWhiteSpace(reason))
                        vars["rejectionReason"] = WebUtility.HtmlEncode(reason);
                    await _emailService.SendTemplatedEmailAsync(templateKey, vars, user.Email);
                }
            }
        }
    }

    private static PolicyDto MapToDto(Policy p)
    {
        return new PolicyDto(
            p.Id,
            p.PolicyNumber,
            p.CustomerId,
            p.ProductId,
            p.Product?.ProductName ?? "",
            p.AgentId,
            p.Status,
            p.PaymentFrequency,
            p.PremiumAmount,
            p.SumAssured,
            "INR",
            p.StartDate,
            p.EndDate,
            p.Product?.Domain?.ToString() ?? "",
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
