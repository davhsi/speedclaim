using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpeedClaim.Api.Dtos.Policies;

namespace SpeedClaim.Api.Interfaces;

public interface IPolicyService
{
    // Customer
    Task<IEnumerable<PolicyDto>> GetMyPoliciesAsync(Guid customerId);
    Task<PolicyDto> GetByIdAsync(Guid policyId, Guid? customerId = null);
    Task<byte[]> DownloadPolicyDocumentAsync(Guid policyId, Guid customerId);
    Task RequestEndorsementAsync(Guid policyId, Guid customerId, RequestEndorsementRequest request);
    Task<IEnumerable<EndorsementDto>> GetPolicyEndorsementsAsync(Guid policyId, Guid customerId);
    Task<IEnumerable<PolicyNomineeDto>> GetNomineesAsync(Guid policyId, Guid customerId);
    Task CancelPolicyAsync(Guid policyId, Guid customerId);

    // Agent
    Task<IEnumerable<PolicyDto>> GetAssignedCustomerPoliciesAsync(Guid agentId);

    // Underwriter / Admin
    Task<IEnumerable<PolicyDto>> GetAllPoliciesAsync();
    Task<IEnumerable<PolicyStatusHistoryDto>> GetPolicyHistoryAsync(Guid policyId, Guid? customerId = null);
    Task ApproveRejectEndorsementAsync(Guid endorsementId, bool isApproved, string reason, Guid underwriterId);
    Task UpdateNomineeAsync(Guid nomineeId, Guid customerId, UpdateNomineeRequest request);
    Task<IEnumerable<EndorsementDto>> GetPendingEndorsementsAsync();
}

public record PolicyNomineeDto(
    Guid Id,
    string FullName,
    string Relationship,
    DateTime DateOfBirth,
    decimal SharePercentage,
    bool IsMinor,
    string? AppointeeName
);

public record PolicyStatusHistoryDto(
    Guid Id,
    Guid PolicyId,
    string Status,
    string? Remarks,
    Guid ChangedById,
    DateTimeOffset ChangedAt
);
