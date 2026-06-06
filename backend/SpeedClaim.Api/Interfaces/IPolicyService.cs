using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpeedClaim.Api.Dtos.Policies;

namespace SpeedClaim.Api.Interfaces;

public interface IPolicyService
{
    Task<PolicyDto> IssuePolicyAsync(CreatePolicyRequest request, Guid? agentId = null);
    Task<PolicyDto> GetPolicyByIdAsync(Guid policyId);
    Task<SpeedClaim.Api.Dtos.Common.PagedResponse<PolicyDto>> GetPoliciesByUserAsync(Guid userId, int pageNumber, int pageSize);
}
