using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpeedClaim.Api.Dtos.Auth;
using SpeedClaim.Api.Dtos.Common;
using SpeedClaim.Api.Dtos.Financial;
using SpeedClaim.Api.Dtos.User;
using SpeedClaim.Api.Exceptions;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;
using SpeedClaim.Api.Models.Enums;

namespace SpeedClaim.Api.Services;

public class AgentService : IAgentService
{
    private readonly IUnitOfWork _unitOfWork;

    public AgentService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private async Task<Models.Agent> ResolveAgentAsync(Guid userId)
    {
        var agent = await _unitOfWork.Agents.FirstOrDefaultAsync(a => a.UserId == userId);
        if (agent == null) throw new NotFoundException("Agent not found.");
        return agent;
    }

    public async Task<IEnumerable<UserDto>> GetAssignedCustomersAsync(string agentId)
    {
        var aId = Guid.Parse(agentId);
        var agent = await ResolveAgentAsync(aId);
        var proposals = await _unitOfWork.Proposals.FindAsync(p => p.AgentId == agent.Id);
        var customerIds = proposals.Select(p => p.CustomerId).Distinct().ToList();

        var customers = new List<UserDto>();
        foreach (var cid in customerIds)
        {
            var customer = await _unitOfWork.Customers.GetByIdAsync(cid);
            if (customer != null)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(customer.UserId);
                if (user != null)
                {
                    customers.Add(new UserDto(
                        user.Id,
                        user.Email,
                        user.Salutation.ToString(),
                        user.FirstName,
                        user.LastName,
                        user.FullName,
                        user.Phone,
                        user.Role.ToString(),
                        customer.MaritalStatus.ToString(),
                        customer.Id,
                        user.IsEmailVerified,
                        user.IsActive,
                        user.CreatedAt,
                        null,
                        null
                    ));
                }
            }
        }
        return customers;
    }

    public async Task<AgentDashboardDto> GetAgentDashboardAsync(string agentId)
    {
        var aId = Guid.Parse(agentId);
        var agent = await ResolveAgentAsync(aId);
        var proposals = await _unitOfWork.Proposals.FindAsync(p => p.AgentId == agent.Id);
        var totalCustomers = proposals.Select(p => p.CustomerId).Distinct().Count();

        var policies = await _unitOfWork.Policies.FindAsync(p => p.Proposal != null && p.Proposal.AgentId == agent.Id);
        var totalPolicies = policies.Count();

        var commissions = await _unitOfWork.AgentCommissions.FindAsync(c => c.AgentId == agent.Id);
        var totalCommission = commissions.Sum(c => c.CommissionAmount);

        var customerIds = proposals.Select(p => p.CustomerId).Distinct().ToList();
        var allClaims = await _unitOfWork.Claims.FindAsync(c => customerIds.Contains(c.CustomerId));
        var pendingClaims = allClaims.Count(c => c.Status != SpeedClaim.Api.Models.Enums.ClaimStatus.Settled && c.Status != SpeedClaim.Api.Models.Enums.ClaimStatus.Rejected);

        return new AgentDashboardDto(totalCustomers, totalPolicies, totalCommission, pendingClaims);
    }

    public async Task<AgentProfileDto> GetAgentProfileAsync(string agentId)
    {
        var aId = Guid.Parse(agentId);
        var agent = await _unitOfWork.Agents.FirstOrDefaultAsync(a => a.UserId == aId);
        if (agent == null) throw new NotFoundException("Agent not found.");

        var user = await _unitOfWork.Users.GetByIdAsync(agent.UserId);
        if (user == null) throw new NotFoundException("User not found.");

        Branch? branch = null;
        if (agent.BranchId.HasValue)
            branch = await _unitOfWork.Branches.GetByIdAsync(agent.BranchId.Value);

        return new AgentProfileDto(
            agent.Id,
            agent.UserId,
            user.Email,
            user.FullName,
            agent.AgentCode,
            agent.AgentType.ToString(),
            agent.LicenseNumber,
            agent.LicenseExpiry,
            agent.CommissionRate,
            agent.IsActive,
            branch?.Name,
            branch?.City
        );
    }

    public async Task<IEnumerable<RenewalReminderDto>> GetRenewalRemindersAsync(string agentId)
    {
        var aId = Guid.Parse(agentId);
        var agent = await ResolveAgentAsync(aId);
        var policies = await _unitOfWork.Policies.FindAsync(p => p.AgentId == agent.Id);
        var policyIds = policies.Select(p => p.Id).ToHashSet();

        if (!policyIds.Any()) return Enumerable.Empty<RenewalReminderDto>();

        // Find upcoming or overdue premium schedules for those policies
        var thirtyDaysFromNow = DateTime.UtcNow.AddDays(30);
        var reminders = new List<RenewalReminderDto>();

        foreach (var policy in policies)
        {
            var schedules = await _unitOfWork.PremiumSchedules.FindAsync(
                s => s.PolicyId == policy.Id &&
                     (s.Status == SpeedClaim.Api.Models.Enums.PremiumScheduleStatus.Upcoming ||
                      s.Status == SpeedClaim.Api.Models.Enums.PremiumScheduleStatus.Overdue) &&
                     s.DueDate <= thirtyDaysFromNow);

            foreach (var schedule in schedules)
            {
                var customer = await _unitOfWork.Customers.GetByIdAsync(policy.CustomerId);
                var user = customer != null ? await _unitOfWork.Users.GetByIdAsync(customer.UserId) : null;

                reminders.Add(new RenewalReminderDto(
                    policy.Id,
                    policy.PolicyNumber,
                    policy.CustomerId,
                    user?.FullName ?? "Unknown",
                    schedule.DueDate,
                    schedule.Amount,
                    (int)(schedule.DueDate - DateTime.UtcNow).TotalDays
                ));
            }
        }

        return reminders.OrderBy(r => r.DaysUntilDue);
    }

    public async Task<IEnumerable<AgentCommissionDto>> GetMyCommissionsAsync(string agentId)
    {
        var aId = Guid.Parse(agentId);
        var agent = await ResolveAgentAsync(aId);
        var commissions = await _unitOfWork.AgentCommissions.FindAsync(c => c.AgentId == agent.Id);
        return commissions.Select(c => new AgentCommissionDto
        {
            Id = c.Id,
            AgentId = c.AgentId,
            PolicyId = c.PolicyId,
            CommissionAmount = c.CommissionAmount,
            Status = c.Status,
            PaidAt = c.PaidAt,
            CreatedAt = c.CreatedAt,
        });
    }

    public async Task<BranchDto> CreateBranchAsync(CreateBranchRequest request, string adminId)
    {
        var branch = new Branch
        {
            Name = request.Name,
            City = request.City,
            State = request.State,
            Address = request.Address,
            Phone = request.Phone,
            Email = request.Email,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _unitOfWork.Branches.AddAsync(branch);
        await _unitOfWork.CompleteAsync();

        return new BranchDto(branch.Id, branch.Name, branch.City, branch.State, branch.Address, branch.Phone, branch.Email, branch.IsActive);
    }

    public async Task<IEnumerable<BranchDto>> GetBranchesAsync()
    {
        var branches = await _unitOfWork.Branches.GetAllAsync();
        return branches.Select(b => new BranchDto(b.Id, b.Name, b.City, b.State, b.Address, b.Phone, b.Email, b.IsActive));
    }

    public async Task AssignAgentToBranchAsync(string agentId, string branchId, string adminId)
    {
        var aId = Guid.Parse(agentId);
        var bId = Guid.Parse(branchId);

        var agent = await _unitOfWork.Agents.FirstOrDefaultAsync(a => a.UserId == aId);
        if (agent == null) throw new NotFoundException("Agent not found");

        var branch = await _unitOfWork.Branches.GetByIdAsync(bId);
        if (branch == null) throw new NotFoundException("Branch not found");

        agent.BranchId = bId;
        agent.UpdatedAt = DateTimeOffset.UtcNow;

        await _unitOfWork.CompleteAsync();
    }

    public async Task UpdateAgentLicenseAsync(string agentId, UpdateAgentLicenseRequest request, string adminId)
    {
        var aId = Guid.Parse(agentId);
        var agent = await _unitOfWork.Agents.FirstOrDefaultAsync(a => a.UserId == aId);
        if (agent == null) throw new NotFoundException("Agent not found");

        agent.LicenseNumber = request.LicenseNumber;
        agent.LicenseExpiry = request.LicenseExpiry;
        agent.UpdatedAt = DateTimeOffset.UtcNow;

        await _unitOfWork.CompleteAsync();
    }

    public async Task ActivateDeactivateAgentAsync(string agentId, bool isActive, string adminId)
    {
        var aId = Guid.Parse(agentId);
        var agent = await _unitOfWork.Agents.FirstOrDefaultAsync(a => a.UserId == aId);
        if (agent == null) throw new NotFoundException("Agent not found");

        agent.IsActive = isActive;
        agent.UpdatedAt = DateTimeOffset.UtcNow;

        await _unitOfWork.CompleteAsync();
    }

    public async Task<IEnumerable<AgentProfileDto>> GetAllAgentsAsync()
    {
        var agents = await _unitOfWork.Agents.GetAllAsync();
        var result = new List<AgentProfileDto>();

        foreach (var agent in agents)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(agent.UserId);
            if (user == null) continue;

            Branch? branch = null;
            if (agent.BranchId.HasValue)
                branch = await _unitOfWork.Branches.GetByIdAsync(agent.BranchId.Value);

            result.Add(new AgentProfileDto(
                agent.Id,
                agent.UserId,
                user.Email,
                user.FullName,
                agent.AgentCode,
                agent.AgentType.ToString(),
                agent.LicenseNumber,
                agent.LicenseExpiry,
                agent.CommissionRate,
                agent.IsActive,
                branch?.Name,
                branch?.City
            ));
        }

        return result;
    }

    public async Task UpdateAgentProfileAsync(string agentId, UpdateAgentProfileRequest request)
    {
        var aId = Guid.Parse(agentId);
        var user = await _unitOfWork.Users.GetByIdAsync(aId);
        if (user == null) throw new NotFoundException("User not found.");

        if (Enum.TryParse<Salutation>(request.Salutation, out var salutation))
            user.Salutation = salutation;

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Phone = request.Phone;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        await _unitOfWork.CompleteAsync();
    }
}
