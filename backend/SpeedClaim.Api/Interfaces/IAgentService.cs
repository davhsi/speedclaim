using System.Collections.Generic;
using System.Threading.Tasks;
using SpeedClaim.Api.Dtos.Auth;
using SpeedClaim.Api.Dtos.Financial;
using SpeedClaim.Api.Dtos.User;
using SpeedClaim.Api.Models.Enums;

namespace SpeedClaim.Api.Interfaces;

public interface IAgentService
{
    Task<IEnumerable<UserDto>> GetAssignedCustomersAsync(string agentId);
    Task EnsureCustomerAssignedAsync(string agentId, string customerUserId);
    Task<AgentDashboardDto> GetAgentDashboardAsync(string agentId);
    Task<AgentProfileDto> GetAgentProfileAsync(string agentId);
    Task UpdateAgentProfileAsync(string agentId, UpdateAgentProfileRequest request);
    Task<IEnumerable<RenewalReminderDto>> GetRenewalRemindersAsync(string agentId);
    Task<IEnumerable<AgentCommissionDto>> GetMyCommissionsAsync(string agentId);

    // Admin - Branch & Agent Management
    Task<BranchDto> CreateBranchAsync(CreateBranchRequest request, string adminId);
    Task<BranchDto> UpdateBranchAsync(string branchId, CreateBranchRequest request, string adminId);
    Task<IEnumerable<BranchDto>> GetBranchesAsync();
    Task AssignAgentToBranchAsync(string agentId, string branchId, string adminId);
    Task UpdateAgentLicenseAsync(string agentId, UpdateAgentLicenseRequest request, string adminId);
    Task ActivateDeactivateAgentAsync(string agentId, bool isActive, string adminId);
    Task<IEnumerable<AgentProfileDto>> GetAllAgentsAsync();
}
