using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
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
    private readonly INotificationService _notifications;
    private readonly IEmailService _emailService;

    public AgentService(IUnitOfWork unitOfWork, INotificationService notifications, IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _notifications = notifications;
        _emailService = emailService;
    }

    private async Task<Models.Agent> ResolveAgentAsync(Guid userId)
    {
        var agent = await _unitOfWork.Agents.FirstOrDefaultAsync(a => a.UserId == userId);
        if (agent == null) throw new NotFoundException("Agent not found.");
        return agent;
    }

    /// <summary>A customer is assigned to an agent via a proposal the agent submitted for them, or by having been directly onboarded by them.</summary>
    private async Task<List<Guid>> GetAssignedCustomerIdsAsync(Guid agentId)
    {
        var proposals = await _unitOfWork.Proposals.FindAsync(p => p.AgentId == agentId);
        var onboardedCustomers = await _unitOfWork.Customers.FindAsync(c => c.OnboardingAgentId == agentId);
        return proposals.Select(p => p.CustomerId)
            .Concat(onboardedCustomers.Select(c => c.Id))
            .Distinct().ToList();
    }

    public async Task<IEnumerable<UserDto>> GetAssignedCustomersAsync(string agentId)
    {
        var aId = Guid.Parse(agentId);
        var agent = await ResolveAgentAsync(aId);
        var customerIds = await GetAssignedCustomerIdsAsync(agent.Id);

        var customers = new List<UserDto>();
        foreach (var cid in customerIds)
        {
            var customer = await _unitOfWork.Customers.GetByIdAsync(cid);
            if (customer != null)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(customer.UserId);
                if (user != null)
                {
                    var kycInfo = await GetKycInfoAsync(user.Id);
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
                        null,
                        null,
                        customer.DateOfBirth,
                        kycInfo.IsApproved,
                        kycInfo.Status,
                        kycInfo.RejectionReason,
                        customer.Occupation,
                        customer.AnnualIncome
                    ));
                }
            }
        }
        return customers;
    }

    private async Task<(bool IsApproved, string? Status, string? RejectionReason)> GetKycInfoAsync(Guid userId)
    {
        var kyc = await _unitOfWork.KycRecords.FirstOrDefaultAsync(k => k.UserId == userId);
        if (kyc == null) return (false, null, null);
        return (kyc.KycStatus == KycStatus.Approved, kyc.KycStatus.ToString(), kyc.RejectionReason);
    }

    public async Task<IEnumerable<UserDto>> SearchCustomersAsync(string? query)
    {
        var customerUsers = await _unitOfWork.Users.FindAsync(u => u.Role == UserRole.Customer);
        IEnumerable<User> matches = customerUsers;

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim().ToLower();
            matches = matches.Where(u =>
                u.FirstName.ToLower().Contains(term) ||
                u.LastName.ToLower().Contains(term) ||
                u.Email.ToLower().Contains(term) ||
                u.Phone.Contains(term));
        }

        var dtos = new List<UserDto>();
        foreach (var user in matches.Take(20))
        {
            var customer = await _unitOfWork.Customers.FirstOrDefaultAsync(c => c.UserId == user.Id);
            if (customer == null) continue;

            var kycInfo = await GetKycInfoAsync(user.Id);
            dtos.Add(new UserDto(
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
                null,
                null,
                customer.DateOfBirth,
                kycInfo.IsApproved,
                kycInfo.Status,
                kycInfo.RejectionReason,
                customer.Occupation,
                customer.AnnualIncome
            ));
        }
        return dtos;
    }

    public async Task EnsureCustomerAssignedAsync(string agentId, string customerUserId)
    {
        var aId = Guid.Parse(agentId);
        var customerUid = Guid.Parse(customerUserId);
        var agent = await ResolveAgentAsync(aId);
        var customer = await _unitOfWork.Customers.FirstOrDefaultAsync(c => c.UserId == customerUid);
        if (customer == null) throw new NotFoundException("Customer not found.");

        var proposal = await _unitOfWork.Proposals.FirstOrDefaultAsync(p =>
            p.AgentId == agent.Id && p.CustomerId == customer.Id);
        var isOnboardedByAgent = customer.OnboardingAgentId == agent.Id;
        if (proposal == null && !isOnboardedByAgent)
            throw new ForbiddenException("Customer is not assigned to this agent.");
    }

    public async Task<AgentDashboardDto> GetAgentDashboardAsync(string agentId)
    {
        var aId = Guid.Parse(agentId);
        var agent = await ResolveAgentAsync(aId);
        var customerIds = await GetAssignedCustomerIdsAsync(agent.Id);
        var totalCustomers = customerIds.Count;

        var policies = await _unitOfWork.Policies.FindAsync(p => p.Proposal != null && p.Proposal.AgentId == agent.Id);
        var totalPolicies = policies.Count();

        var commissions = await _unitOfWork.AgentCommissions.FindAsync(c => c.AgentId == agent.Id);
        var totalCommission = commissions.Sum(c => c.CommissionAmount);

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
            user.Salutation.ToString(),
            user.FirstName,
            user.LastName,
            user.FullName,
            user.Phone,
            agent.AgentCode,
            agent.AgentType.ToString(),
            agent.LicenseNumber,
            agent.LicenseExpiry,
            agent.CommissionRate,
            agent.IsActive,
            branch?.Name,
            branch?.City,
            agent.BranchId
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
        var reminderCutoff = DateTime.UtcNow.AddHours(-24);
        var recentReminderPolicyIds = (await _unitOfWork.AuditLogs.FindAsync(a =>
                a.EntityType == "Policy" &&
                a.UserId == aId &&
                a.Action == "PremiumRenewalReminderSent" &&
                a.CreatedAt >= reminderCutoff &&
                policyIds.Contains(a.EntityId)))
            .Select(a => a.EntityId)
            .ToHashSet();
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
                    user?.Email ?? string.Empty,
                    user?.Phone ?? string.Empty,
                    schedule.DueDate,
                    schedule.Amount,
                    (int)(schedule.DueDate - DateTime.UtcNow).TotalDays,
                    recentReminderPolicyIds.Contains(policy.Id)
                ));
            }
        }

        return reminders.OrderBy(r => r.DaysUntilDue);
    }

    public async Task SendRenewalReminderAsync(string agentId, string policyId)
    {
        var aId = Guid.Parse(agentId);
        var pId = Guid.Parse(policyId);
        var agent = await ResolveAgentAsync(aId);

        var policy = await _unitOfWork.Policies.GetByIdAsync(pId)
            ?? throw new NotFoundException("Policy not found.");
        if (policy.AgentId != agent.Id)
            throw new ForbiddenException("Access denied to this policy.");

        var schedules = await _unitOfWork.PremiumSchedules.FindAsync(s =>
            s.PolicyId == policy.Id &&
            (s.Status == PremiumScheduleStatus.Upcoming ||
             s.Status == PremiumScheduleStatus.Due ||
             s.Status == PremiumScheduleStatus.Overdue));
        var schedule = schedules.OrderBy(s => s.DueDate).FirstOrDefault()
            ?? throw new NotFoundException("No unpaid premium schedule found for this policy.");

        var reminderCutoff = DateTime.UtcNow.AddHours(-24);
        var recentReminders = await _unitOfWork.AuditLogs.FindAsync(a =>
            a.EntityType == "Policy" &&
            a.EntityId == policy.Id &&
            a.UserId == aId &&
            a.Action == "PremiumRenewalReminderSent" &&
            a.CreatedAt >= reminderCutoff);
        if (recentReminders.Any())
            throw new ConflictException("A premium reminder was already sent for this policy in the last 24 hours.");

        var customer = await _unitOfWork.Customers.GetByIdAsync(policy.CustomerId)
            ?? throw new NotFoundException("Customer not found.");
        var user = await _unitOfWork.Users.GetByIdAsync(customer.UserId)
            ?? throw new NotFoundException("Customer account not found.");

        var isOverdue = schedule.Status == PremiumScheduleStatus.Overdue || schedule.DueDate.Date < DateTime.UtcNow.Date;
        await _notifications.CreateAsync(
            user.Id,
            isOverdue ? "Premium overdue" : "Premium due soon",
            $"Your policy {policy.PolicyNumber} has a premium of INR {schedule.Amount:0.00} due on {schedule.DueDate:dd MMM yyyy}. Pay now to keep your cover active.",
            "payment",
            $"/pay/{policy.Id}");

        var payUrl = $"/pay/{policy.Id}";
        await _emailService.SendEmailAsync(
            user.Email,
            $"Premium reminder for policy {policy.PolicyNumber}",
            $@"<p>Dear {WebUtility.HtmlEncode(user.FirstName)},</p>
<p>Your SpeedClaim policy <strong>{WebUtility.HtmlEncode(policy.PolicyNumber)}</strong> has a premium of <strong>INR {schedule.Amount:0.00}</strong> due on <strong>{schedule.DueDate:dd MMM yyyy}</strong>.</p>
<p>Please log in to SpeedClaim and pay from <strong>{WebUtility.HtmlEncode(payUrl)}</strong> to keep your cover active.</p>
<p>Regards,<br/>SpeedClaim</p>");

        await _unitOfWork.AuditLogs.AddAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = aId,
            EntityType = "Policy",
            EntityId = policy.Id,
            Action = "PremiumRenewalReminderSent",
            NewValue = JsonSerializer.Serialize(new { policy.PolicyNumber, customerUserId = user.Id, scheduleId = schedule.Id, amount = schedule.Amount }),
            CreatedAt = DateTime.UtcNow
        });
        await _unitOfWork.CompleteAsync();
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

    public async Task<BranchDto> UpdateBranchAsync(string branchId, CreateBranchRequest request, string adminId)
    {
        if (!Guid.TryParse(branchId, out var bId))
            throw new NotFoundException("Branch not found");

        var branch = await _unitOfWork.Branches.GetByIdAsync(bId)
            ?? throw new NotFoundException("Branch not found");

        branch.Name = request.Name;
        branch.City = request.City;
        branch.State = request.State;
        branch.Address = request.Address;
        branch.Phone = request.Phone;
        branch.Email = request.Email;
        branch.UpdatedAt = DateTimeOffset.UtcNow;

        _unitOfWork.Branches.Update(branch);
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
        await _unitOfWork.AuditLogs.AddAsync(new AuditLog
        {
            Id = Guid.NewGuid(), UserId = Guid.Parse(adminId), EntityType = "Agent", EntityId = agent.Id,
            Action = "AgentBranchAssigned",
            NewValue = JsonSerializer.Serialize(new { branchId = bId, branchName = branch.Name }),
            CreatedAt = DateTime.UtcNow
        });

        await _unitOfWork.CompleteAsync();
    }

    public async Task UpdateAgentLicenseAsync(string agentId, UpdateAgentLicenseRequest request, string adminId)
    {
        var aId = Guid.Parse(agentId);
        var agent = await _unitOfWork.Agents.FirstOrDefaultAsync(a => a.UserId == aId);
        if (agent == null) throw new NotFoundException("Agent not found");

        var existingLicense = await _unitOfWork.Agents.FirstOrDefaultAsync(a => a.Id != agent.Id && a.LicenseNumber.ToUpper() == request.LicenseNumber.ToUpper());
        if (existingLicense != null)
            throw new ConflictException("License number already registered to another agent");

        agent.LicenseNumber = request.LicenseNumber;
        agent.LicenseExpiry = request.LicenseExpiry;
        agent.UpdatedAt = DateTimeOffset.UtcNow;
        await _unitOfWork.AuditLogs.AddAsync(new AuditLog
        {
            Id = Guid.NewGuid(), UserId = Guid.Parse(adminId), EntityType = "Agent", EntityId = agent.Id,
            Action = "AgentLicenseUpdated",
            NewValue = JsonSerializer.Serialize(new { licenseNumber = request.LicenseNumber, licenseExpiry = request.LicenseExpiry }),
            CreatedAt = DateTime.UtcNow
        });

        await _unitOfWork.CompleteAsync();
    }

    public async Task ActivateDeactivateAgentAsync(string agentId, bool isActive, string adminId)
    {
        var aId = Guid.Parse(agentId);
        var agent = await _unitOfWork.Agents.FirstOrDefaultAsync(a => a.UserId == aId);
        if (agent == null) throw new NotFoundException("Agent not found");

        // The admin agent-management list, and JWT validation on every request (Program.cs
        // OnTokenValidated), both key off User.IsActive — not Agent.IsActive. Flipping only
        // Agent.IsActive left the list reverting to "Active" on refresh and, more importantly,
        // never actually revoked the agent's access.
        var user = await _unitOfWork.Users.GetByIdAsync(agent.UserId);
        if (user == null) throw new NotFoundException("Agent not found");

        agent.IsActive = isActive;
        agent.UpdatedAt = DateTimeOffset.UtcNow;
        user.IsActive = isActive;
        await _unitOfWork.AuditLogs.AddAsync(new AuditLog
        {
            Id = Guid.NewGuid(), UserId = Guid.Parse(adminId), EntityType = "Agent", EntityId = agent.Id,
            Action = isActive ? "AgentActivated" : "AgentDeactivated",
            CreatedAt = DateTime.UtcNow
        });

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
                user.Salutation.ToString(),
                user.FirstName,
                user.LastName,
                user.FullName,
                user.Phone,
                agent.AgentCode,
                agent.AgentType.ToString(),
                agent.LicenseNumber,
                agent.LicenseExpiry,
                agent.CommissionRate,
                agent.IsActive,
                branch?.Name,
                branch?.City,
                agent.BranchId
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

        await _unitOfWork.AuditLogs.AddAsync(new AuditLog
        {
            Id = Guid.NewGuid(), UserId = aId, EntityType = "Agent", EntityId = aId,
            Action = "AgentProfileUpdated",
            NewValue = JsonSerializer.Serialize(new { firstName = request.FirstName, lastName = request.LastName, phone = request.Phone }),
            CreatedAt = DateTime.UtcNow
        });

        await _unitOfWork.CompleteAsync();
    }
}
