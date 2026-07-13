using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SpeedClaim.Api.Dtos.User;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;
using SpeedClaim.Api.Services;
using SpeedClaim.Api.Models.Enums;

namespace SpeedClaim.Tests.Services;

[TestFixture]
public class AgentServiceTests
{
    private Mock<IUnitOfWork> _mockUnitOfWork = null!;
    private Mock<IRepository<Agent>> _mockAgentRepo = null!;
    private Mock<IRepository<Branch>> _mockBranchRepo = null!;
    private Mock<IRepository<Proposal>> _mockProposalRepo = null!;
    private Mock<IRepository<Customer>> _mockCustomerRepo = null!;
    private Mock<IUserRepository> _mockUserRepo = null!;
    private Mock<IPolicyRepository> _mockPolicyRepo = null!;
    private Mock<IRepository<AgentCommission>> _mockCommissionRepo = null!;
    private Mock<IClaimRepository> _mockClaimRepo = null!;
    private Mock<IRepository<KycRecord>> _mockKycRepo = null!;
    private Mock<IRepository<PremiumSchedule>> _mockPremiumScheduleRepo = null!;
    private Mock<IRepository<AuditLog>> _mockAuditRepo = null!;
    private Mock<INotificationService> _mockNotifications = null!;
    private Mock<IEmailService> _mockEmailService = null!;
    private AgentService _agentService = null!;

    [SetUp]
    public void Setup()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockAgentRepo = new Mock<IRepository<Agent>>();
        _mockBranchRepo = new Mock<IRepository<Branch>>();
        _mockProposalRepo = new Mock<IRepository<Proposal>>();
        _mockCustomerRepo = new Mock<IRepository<Customer>>();
        _mockUserRepo = new Mock<IUserRepository>();
        _mockPolicyRepo = new Mock<IPolicyRepository>();
        _mockCommissionRepo = new Mock<IRepository<AgentCommission>>();
        _mockClaimRepo = new Mock<IClaimRepository>();
        _mockKycRepo = new Mock<IRepository<KycRecord>>();
        _mockPremiumScheduleRepo = new Mock<IRepository<PremiumSchedule>>();
        _mockAuditRepo = new Mock<IRepository<AuditLog>>();
        _mockNotifications = new Mock<INotificationService>();
        _mockEmailService = new Mock<IEmailService>();

        _mockUnitOfWork.Setup(u => u.KycRecords).Returns(_mockKycRepo.Object);
        _mockUnitOfWork.Setup(u => u.Agents).Returns(_mockAgentRepo.Object);
        _mockUnitOfWork.Setup(u => u.Branches).Returns(_mockBranchRepo.Object);
        _mockUnitOfWork.Setup(u => u.Proposals).Returns(_mockProposalRepo.Object);
        _mockUnitOfWork.Setup(u => u.Customers).Returns(_mockCustomerRepo.Object);
        _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepo.Object);
        _mockUnitOfWork.Setup(u => u.Policies).Returns(_mockPolicyRepo.Object);
        _mockUnitOfWork.Setup(u => u.AgentCommissions).Returns(_mockCommissionRepo.Object);
        _mockUnitOfWork.Setup(u => u.Claims).Returns(_mockClaimRepo.Object);
        _mockUnitOfWork.Setup(u => u.PremiumSchedules).Returns(_mockPremiumScheduleRepo.Object);
        _mockUnitOfWork.Setup(u => u.AuditLogs).Returns(_mockAuditRepo.Object);
        _mockAuditRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<AuditLog, bool>>>()))
            .ReturnsAsync(Array.Empty<AuditLog>());
        _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

        _agentService = new AgentService(_mockUnitOfWork.Object, _mockNotifications.Object, _mockEmailService.Object);
    }

    [Test]
    public async Task GetAssignedCustomersAsync_ReturnsCustomerList()
    {
        var agentId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var proposals = new List<Proposal> { new Proposal { AgentId = agentId, CustomerId = customerId } };
        var customer = new Customer { Id = customerId, UserId = userId, MaritalStatus = MaritalStatus.Married };
        var user = new User { Id = userId, Email = "test@test.com", Role = UserRole.Customer, Salutation = Salutation.Mr, FirstName = "John", LastName = "Doe" };

        _mockAgentRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Agent, bool>>>())).ReturnsAsync(new Agent { Id = agentId, UserId = agentId });
        _mockProposalRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Proposal, bool>>>())).ReturnsAsync(proposals);
        _mockCustomerRepo.Setup(r => r.GetByIdAsync(customerId)).ReturnsAsync(customer);
        _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        _mockKycRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<KycRecord, bool>>>()))
            .ReturnsAsync(new KycRecord { UserId = userId, KycStatus = KycStatus.Approved });

        var result = await _agentService.GetAssignedCustomersAsync(agentId.ToString());

        var resultList = result.ToList();
        Assert.That(resultList.Count, Is.EqualTo(1));
        Assert.That(resultList[0].KycApproved, Is.True);
        Assert.That(resultList[0].KycStatus, Is.EqualTo("Approved"));
        Assert.That(resultList[0].Email, Is.EqualTo("test@test.com"));
    }

    [Test]
    public async Task GetAssignedCustomersAsync_RejectedKyc_IncludesStatusAndReason()
    {
        var agentId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var proposals = new List<Proposal> { new Proposal { AgentId = agentId, CustomerId = customerId } };
        var customer = new Customer { Id = customerId, UserId = userId, MaritalStatus = MaritalStatus.Married };
        var user = new User { Id = userId, Email = "test@test.com", Role = UserRole.Customer, Salutation = Salutation.Mr, FirstName = "John", LastName = "Doe" };

        _mockAgentRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Agent, bool>>>())).ReturnsAsync(new Agent { Id = agentId, UserId = agentId });
        _mockProposalRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Proposal, bool>>>())).ReturnsAsync(proposals);
        _mockCustomerRepo.Setup(r => r.GetByIdAsync(customerId)).ReturnsAsync(customer);
        _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockKycRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<KycRecord, bool>>>()))
            .ReturnsAsync(new KycRecord { UserId = userId, KycStatus = KycStatus.Rejected, RejectionReason = "Blurry Aadhaar photo" });

        var result = (await _agentService.GetAssignedCustomersAsync(agentId.ToString())).ToList();

        Assert.That(result[0].KycApproved, Is.False);
        Assert.That(result[0].KycStatus, Is.EqualTo("Rejected"));
        Assert.That(result[0].KycRejectionReason, Is.EqualTo("Blurry Aadhaar photo"));
    }

    [Test]
    public async Task GetAssignedCustomersAsync_IncludesOnboardedCustomerWithNoProposal()
    {
        var agentId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var customer = new Customer { Id = customerId, UserId = userId, MaritalStatus = MaritalStatus.Single, OnboardingAgentId = agentId };
        var user = new User { Id = userId, Email = "onboarded@test.com", Role = UserRole.Customer, Salutation = Salutation.Ms, FirstName = "Priya", LastName = "Nair" };

        _mockAgentRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Agent, bool>>>())).ReturnsAsync(new Agent { Id = agentId, UserId = agentId });
        _mockProposalRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Proposal, bool>>>())).ReturnsAsync(new List<Proposal>());
        _mockCustomerRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Customer, bool>>>())).ReturnsAsync(new List<Customer> { customer });
        _mockCustomerRepo.Setup(r => r.GetByIdAsync(customerId)).ReturnsAsync(customer);
        _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        var result = (await _agentService.GetAssignedCustomersAsync(agentId.ToString())).ToList();

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].Email, Is.EqualTo("onboarded@test.com"));
        Assert.That(result[0].KycApproved, Is.False); // no KycRecord mocked — never submitted KYC
        Assert.That(result[0].KycStatus, Is.Null);
    }

    [Test]
    public async Task SearchCustomersAsync_NoQuery_ReturnsAllCustomerUsers()
    {
        var userId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "priya@test.com", Role = UserRole.Customer, Salutation = Salutation.Ms, FirstName = "Priya", LastName = "Sharma", Phone = "9876543210" };
        var customer = new Customer { Id = customerId, UserId = userId, MaritalStatus = MaritalStatus.Single };

        _mockUserRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(new List<User> { user });
        _mockCustomerRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Customer, bool>>>())).ReturnsAsync(customer);
        _mockKycRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<KycRecord, bool>>>()))
            .ReturnsAsync(new KycRecord { UserId = userId, KycStatus = KycStatus.Approved });

        var result = (await _agentService.SearchCustomersAsync(null)).ToList();

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].Email, Is.EqualTo("priya@test.com"));
        Assert.That(result[0].KycApproved, Is.True);
        Assert.That(result[0].KycStatus, Is.EqualTo("Approved"));
    }

    [Test]
    public async Task SearchCustomersAsync_WithQuery_FiltersCaseInsensitively()
    {
        var matchUser = new User { Id = Guid.NewGuid(), Email = "priya@test.com", Role = UserRole.Customer, Salutation = Salutation.Ms, FirstName = "Priya", LastName = "Sharma", Phone = "9876543210" };
        var otherUser = new User { Id = Guid.NewGuid(), Email = "arjun@test.com", Role = UserRole.Customer, Salutation = Salutation.Mr, FirstName = "Arjun", LastName = "Nair", Phone = "9123456780" };
        var customer = new Customer { Id = Guid.NewGuid(), UserId = matchUser.Id, MaritalStatus = MaritalStatus.Single };

        _mockUserRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(new List<User> { matchUser, otherUser });
        _mockCustomerRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Customer, bool>>>())).ReturnsAsync(customer);

        var result = (await _agentService.SearchCustomersAsync("PRIYA")).ToList();

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].Email, Is.EqualTo("priya@test.com"));
    }

    [Test]
    public async Task EnsureCustomerAssignedAsync_AssignedCustomer_Succeeds()
    {
        var agentUserId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var customerUserId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        _mockAgentRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Agent, bool>>>()))
            .ReturnsAsync(new Agent { Id = agentId, UserId = agentUserId });
        _mockCustomerRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Customer, bool>>>()))
            .ReturnsAsync(new Customer { Id = customerId, UserId = customerUserId });
        _mockProposalRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Proposal, bool>>>()))
            .ReturnsAsync(new Proposal { AgentId = agentId, CustomerId = customerId });

        await _agentService.EnsureCustomerAssignedAsync(agentUserId.ToString(), customerUserId.ToString());
    }

    [Test]
    public async Task EnsureCustomerAssignedAsync_OnboardedByAgentWithNoProposal_Succeeds()
    {
        var agentUserId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var customerUserId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        _mockAgentRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Agent, bool>>>()))
            .ReturnsAsync(new Agent { Id = agentId, UserId = agentUserId });
        _mockCustomerRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Customer, bool>>>()))
            .ReturnsAsync(new Customer { Id = customerId, UserId = customerUserId, OnboardingAgentId = agentId });
        _mockProposalRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Proposal, bool>>>()))
            .ReturnsAsync((Proposal?)null);

        await _agentService.EnsureCustomerAssignedAsync(agentUserId.ToString(), customerUserId.ToString());
    }

    [Test]
    public void EnsureCustomerAssignedAsync_UnassignedCustomer_ThrowsForbiddenException()
    {
        var agentUserId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var customerUserId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        _mockAgentRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Agent, bool>>>()))
            .ReturnsAsync(new Agent { Id = agentId, UserId = agentUserId });
        _mockCustomerRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Customer, bool>>>()))
            .ReturnsAsync(new Customer { Id = customerId, UserId = customerUserId });
        _mockProposalRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Proposal, bool>>>()))
            .ReturnsAsync((Proposal?)null);

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ForbiddenException>(() =>
            _agentService.EnsureCustomerAssignedAsync(agentUserId.ToString(), customerUserId.ToString()));
    }

    [Test]
    public async Task GetAgentDashboardAsync_ReturnsDashboardStats()
    {
        var agentId = Guid.NewGuid();
        var proposals = new List<Proposal> { new Proposal { AgentId = agentId, CustomerId = Guid.NewGuid() } };
        var policies = new List<Policy> { new Policy { Id = Guid.NewGuid() } };
        var commissions = new List<AgentCommission> { new AgentCommission { CommissionAmount = 500 } };

        _mockAgentRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Agent, bool>>>())).ReturnsAsync(new Agent { Id = agentId, UserId = agentId });
        _mockProposalRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Proposal, bool>>>())).ReturnsAsync(proposals);
        _mockPolicyRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Policy, bool>>>())).ReturnsAsync(policies);
        _mockCommissionRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<AgentCommission, bool>>>())).ReturnsAsync(commissions);
        _mockClaimRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Claim, bool>>>())).ReturnsAsync(new List<Claim>());

        var result = await _agentService.GetAgentDashboardAsync(agentId.ToString());

        Assert.That(result.TotalCustomers, Is.EqualTo(1));
        Assert.That(result.TotalPolicies, Is.EqualTo(1));
        Assert.That(result.TotalCommission, Is.EqualTo(500));
    }

    [Test]
    public async Task CreateBranchAsync_Success()
    {
        var request = new CreateBranchRequest("Branch A", "City A", "State A", "Address A", "123", "a@a.com");
        var adminId = Guid.NewGuid().ToString();

        _mockBranchRepo.Setup(r => r.AddAsync(It.IsAny<Branch>())).Returns(Task.CompletedTask);

        var result = await _agentService.CreateBranchAsync(request, adminId);

        Assert.That(result.Name, Is.EqualTo("Branch A"));
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public async Task GetBranchesAsync_ReturnsList()
    {
        var branches = new List<Branch> { new Branch { Name = "Branch 1" } };
        _mockBranchRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(branches);

        var result = await _agentService.GetBranchesAsync();

        Assert.That(result.Count(), Is.EqualTo(1));
        Assert.That(result.First().Name, Is.EqualTo("Branch 1"));
    }

    [Test]
    public void AssignAgentToBranchAsync_AgentNotFound_ThrowsException()
    {
        _mockAgentRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Agent, bool>>>())).ReturnsAsync((Agent?)null);

        var ex = Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.NotFoundException>(() => _agentService.AssignAgentToBranchAsync(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString()));
        Assert.That(ex.Message, Is.EqualTo("Agent not found"));
    }

    [Test]
    public void AssignAgentToBranchAsync_BranchNotFound_ThrowsException()
    {
        var agent = new Agent();
        _mockAgentRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Agent, bool>>>())).ReturnsAsync(agent);
        _mockBranchRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Branch?)null);

        var ex = Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.NotFoundException>(() => _agentService.AssignAgentToBranchAsync(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString()));
        Assert.That(ex.Message, Is.EqualTo("Branch not found"));
    }

    [Test]
    public async Task AssignAgentToBranchAsync_Success()
    {
        var agentId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var agent = new Agent();
        var branch = new Branch();

        _mockAgentRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Agent, bool>>>())).ReturnsAsync(agent);
        _mockBranchRepo.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(branch);

        await _agentService.AssignAgentToBranchAsync(agentId.ToString(), branchId.ToString(), Guid.NewGuid().ToString());

        Assert.That(agent.BranchId, Is.EqualTo(branchId));
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public void UpdateAgentLicenseAsync_AgentNotFound_ThrowsException()
    {
        _mockAgentRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Agent, bool>>>())).ReturnsAsync((Agent?)null);
        var request = new UpdateAgentLicenseRequest("LIC-123", DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)));

        var ex = Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.NotFoundException>(() => _agentService.UpdateAgentLicenseAsync(Guid.NewGuid().ToString(), request, Guid.NewGuid().ToString()));
        Assert.That(ex.Message, Is.EqualTo("Agent not found"));
    }

    [Test]
    public async Task UpdateAgentLicenseAsync_Success()
    {
        var agentId = Guid.NewGuid();
        var agent = new Agent();
        var expiry = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1));
        var request = new UpdateAgentLicenseRequest("LIC-123", expiry);

        // First call resolves the agent being updated; second call is the duplicate-license check.
        _mockAgentRepo.SetupSequence(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Agent, bool>>>()))
            .ReturnsAsync(agent)
            .ReturnsAsync((Agent?)null);

        await _agentService.UpdateAgentLicenseAsync(agentId.ToString(), request, Guid.NewGuid().ToString());

        Assert.That(agent.LicenseNumber, Is.EqualTo("LIC-123"));
        Assert.That(agent.LicenseExpiry, Is.EqualTo(expiry));
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public void UpdateAgentLicenseAsync_LicenseNumberAlreadyUsedByAnotherAgent_ThrowsConflictException()
    {
        var agentId = Guid.NewGuid();
        var agent = new Agent { Id = Guid.NewGuid(), LicenseNumber = "LIC-OLD" };
        var otherAgent = new Agent { Id = Guid.NewGuid(), LicenseNumber = "lic-123" };
        var request = new UpdateAgentLicenseRequest("LIC-123", DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)));

        _mockAgentRepo.SetupSequence(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Agent, bool>>>()))
            .ReturnsAsync(agent)
            .ReturnsAsync(otherAgent);

        var ex = Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ConflictException>(() => _agentService.UpdateAgentLicenseAsync(agentId.ToString(), request, Guid.NewGuid().ToString()));
        Assert.That(ex.Message, Is.EqualTo("License number already registered to another agent"));
        Assert.That(agent.LicenseNumber, Is.EqualTo("LIC-OLD"), "must not mutate the agent before the duplicate check passes");
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Never);
    }

    [Test]
    public void ActivateDeactivateAgentAsync_AgentNotFound_ThrowsException()
    {
        _mockAgentRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Agent, bool>>>())).ReturnsAsync((Agent?)null);

        var ex = Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.NotFoundException>(() => _agentService.ActivateDeactivateAgentAsync(Guid.NewGuid().ToString(), false, Guid.NewGuid().ToString()));
        Assert.That(ex.Message, Is.EqualTo("Agent not found"));
    }

    [Test]
    public async Task ActivateDeactivateAgentAsync_Success()
    {
        var agentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var agent = new Agent { IsActive = true, UserId = userId };
        var user = new User { Id = userId, IsActive = true };

        _mockAgentRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Agent, bool>>>())).ReturnsAsync(agent);
        _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        await _agentService.ActivateDeactivateAgentAsync(agentId.ToString(), false, Guid.NewGuid().ToString());

        Assert.That(agent.IsActive, Is.False);
        Assert.That(user.IsActive, Is.False, "must also flip User.IsActive — it's what the agent list and JWT validation actually read");
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public void ActivateDeactivateAgentAsync_UserRecordMissing_ThrowsException()
    {
        var agentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var agent = new Agent { IsActive = true, UserId = userId };

        _mockAgentRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Agent, bool>>>())).ReturnsAsync(agent);
        _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        var ex = Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.NotFoundException>(() => _agentService.ActivateDeactivateAgentAsync(agentId.ToString(), false, Guid.NewGuid().ToString()));
        Assert.That(ex.Message, Is.EqualTo("Agent not found"));
    }

    // --- GetAgentProfileAsync tests ---

    [Test]
    public void GetAgentProfileAsync_AgentNotFound_ThrowsKeyNotFound()
    {
        var userId = Guid.NewGuid();
        _mockAgentRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Agent, bool>>>())).ReturnsAsync((Agent?)null);

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.NotFoundException>(() => _agentService.GetAgentProfileAsync(userId.ToString()));
    }

    [Test]
    public async Task GetAgentProfileAsync_Success_ReturnsProfile()
    {
        var userId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var expiry = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(2));

        var agent = new Agent
        {
            Id = agentId,
            UserId = userId,
            AgentCode = "AG-001",
            AgentType = AgentType.Internal,
            LicenseNumber = "LIC-999",
            LicenseExpiry = expiry,
            CommissionRate = 0.10m,
            IsActive = true
        };
        var user = new User
        {
            Id = userId,
            Email = "agent@test.com",
            FirstName = "Alice",
            LastName = "Smith",
            Role = UserRole.Agent,
            Salutation = Salutation.Ms
        };

        _mockAgentRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Agent, bool>>>())).ReturnsAsync(agent);
        _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockBranchRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Branch?)null);

        var result = await _agentService.GetAgentProfileAsync(userId.ToString());

        Assert.That(result.AgentCode, Is.EqualTo("AG-001"));
        Assert.That(result.Email, Is.EqualTo("agent@test.com"));
        Assert.That(result.LicenseNumber, Is.EqualTo("LIC-999"));
        Assert.That(result.LicenseExpiry, Is.EqualTo(expiry));
        Assert.That(result.IsActive, Is.True);
        Assert.That(result.BranchName, Is.Null);
    }

    [Test]
    public async Task GetAgentProfileAsync_WithBranch_ReturnsBranchDetails()
    {
        var userId = Guid.NewGuid();
        var branchId = Guid.NewGuid();

        var agent = new Agent { Id = Guid.NewGuid(), UserId = userId, BranchId = branchId };
        var user = new User { Id = userId, Email = "a@b.com", FirstName = "X", LastName = "Y", Role = UserRole.Agent, Salutation = Salutation.Mr };
        var branch = new Branch { Id = branchId, Name = "North Branch", City = "Chicago" };

        _mockAgentRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Agent, bool>>>())).ReturnsAsync(agent);
        _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockBranchRepo.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(branch);

        var result = await _agentService.GetAgentProfileAsync(userId.ToString());

        Assert.That(result.BranchName, Is.EqualTo("North Branch"));
        Assert.That(result.BranchCity, Is.EqualTo("Chicago"));
    }

    // --- UpdateAgentProfileAsync tests ---

    [Test]
    public async Task UpdateAgentProfileAsync_Success_UpdatesUserFields()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, FirstName = "Old", LastName = "Name", Phone = "111", Salutation = Salutation.Mr };

        _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        var request = new UpdateAgentProfileRequest("Ms", "New", "Name", "999");

        await _agentService.UpdateAgentProfileAsync(userId.ToString(), request);

        Assert.That(user.FirstName, Is.EqualTo("New"));
        Assert.That(user.Phone, Is.EqualTo("999"));
        Assert.That(user.Salutation, Is.EqualTo(Salutation.Ms));
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public void UpdateAgentProfileAsync_UserNotFound_ThrowsException()
    {
        _mockUserRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.NotFoundException>(() =>
            _agentService.UpdateAgentProfileAsync(Guid.NewGuid().ToString(), new UpdateAgentProfileRequest("Mr", "A", "B", "123")));
    }

    // --- GetAllAgentsAsync tests ---

    [Test]
    public async Task GetAllAgentsAsync_ReturnsAllAgentProfiles()
    {
        var userId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var agents = new List<Agent>
        {
            new Agent { Id = agentId, UserId = userId, AgentCode = "AG-001", AgentType = AgentType.Internal, LicenseNumber = "LIC-1", LicenseExpiry = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)), CommissionRate = 0.10m, IsActive = true }
        };
        var user = new User { Id = userId, Email = "agent@test.com", FirstName = "Alice", LastName = "Smith", Role = UserRole.Agent, Salutation = Salutation.Ms };

        _mockAgentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(agents);
        _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockBranchRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Branch?)null);

        var result = (await _agentService.GetAllAgentsAsync()).ToList();

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].AgentCode, Is.EqualTo("AG-001"));
        Assert.That(result[0].Email, Is.EqualTo("agent@test.com"));
    }

    [Test]
    public async Task GetAllAgentsAsync_NoAgents_ReturnsEmpty()
    {
        _mockAgentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Agent>());

        var result = (await _agentService.GetAllAgentsAsync()).ToList();

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetAllAgentsAsync_WithBranch_IncludesBranchDetails()
    {
        var userId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var agents = new List<Agent>
        {
            new Agent { Id = Guid.NewGuid(), UserId = userId, BranchId = branchId, AgentCode = "AG-002" }
        };
        var user = new User { Id = userId, Email = "a@b.com", FirstName = "X", LastName = "Y", Role = UserRole.Agent, Salutation = Salutation.Mr };
        var branch = new Branch { Id = branchId, Name = "North Branch", City = "Chicago" };

        _mockAgentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(agents);
        _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockBranchRepo.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(branch);

        var result = (await _agentService.GetAllAgentsAsync()).ToList();

        Assert.That(result[0].BranchName, Is.EqualTo("North Branch"));
        Assert.That(result[0].BranchCity, Is.EqualTo("Chicago"));
    }

    // --- GetRenewalRemindersAsync tests ---

    [Test]
    public async Task GetRenewalRemindersAsync_NoPolicies_ReturnsEmpty()
    {
        var agentId = Guid.NewGuid();
        _mockAgentRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Agent, bool>>>())).ReturnsAsync(new Agent { Id = agentId, UserId = agentId });
        _mockPolicyRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Policy, bool>>>()))
            .ReturnsAsync(new List<Policy>());

        var result = await _agentService.GetRenewalRemindersAsync(agentId.ToString());

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetRenewalRemindersAsync_WithUpcomingSchedules_ReturnsReminders()
    {
        var agentId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _mockAgentRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Agent, bool>>>())).ReturnsAsync(new Agent { Id = agentId, UserId = agentId });

        var policies = new List<Policy>
        {
            new Policy { Id = policyId, AgentId = agentId, CustomerId = customerId, PolicyNumber = "POL-100" }
        };
        var schedules = new List<PremiumSchedule>
        {
            new PremiumSchedule
            {
                Id = Guid.NewGuid(),
                PolicyId = policyId,
                Amount = 300,
                Status = PremiumScheduleStatus.Upcoming,
                DueDate = DateTime.UtcNow.AddDays(10)
            }
        };
        var customer = new Customer { Id = customerId, UserId = userId };
        var user = new User { Id = userId, FirstName = "Bob", LastName = "Jones", Email = "b@j.com", Role = UserRole.Customer, Salutation = Salutation.Mr };

        var mockScheduleRepo = new Mock<IRepository<PremiumSchedule>>();
        _mockUnitOfWork.Setup(u => u.PremiumSchedules).Returns(mockScheduleRepo.Object);

        _mockPolicyRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Policy, bool>>>())).ReturnsAsync(policies);
        mockScheduleRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<PremiumSchedule, bool>>>())).ReturnsAsync(schedules);
        _mockCustomerRepo.Setup(r => r.GetByIdAsync(customerId)).ReturnsAsync(customer);
        _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        var result = (await _agentService.GetRenewalRemindersAsync(agentId.ToString())).ToList();

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].PolicyNumber, Is.EqualTo("POL-100"));
        Assert.That(result[0].AmountDue, Is.EqualTo(300));
        Assert.That(result[0].DaysUntilDue, Is.InRange(9, 10)); // account for small timing differences
        Assert.That(result[0].ReminderSentRecently, Is.False);
    }

    [Test]
    public async Task GetRenewalRemindersAsync_WithRecentReminder_MarksReminderSentRecently()
    {
        var agentUserId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _mockAgentRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Agent, bool>>>()))
            .ReturnsAsync(new Agent { Id = agentId, UserId = agentUserId });
        _mockPolicyRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Policy, bool>>>()))
            .ReturnsAsync(new[] { new Policy { Id = policyId, AgentId = agentId, CustomerId = customerId, PolicyNumber = "POL-100" } });
        _mockPremiumScheduleRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<PremiumSchedule, bool>>>()))
            .ReturnsAsync(new[]
            {
                new PremiumSchedule
                {
                    Id = Guid.NewGuid(),
                    PolicyId = policyId,
                    Amount = 300,
                    Status = PremiumScheduleStatus.Upcoming,
                    DueDate = DateTime.UtcNow.AddDays(10)
                }
            });
        _mockAuditRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<AuditLog, bool>>>()))
            .ReturnsAsync(new[] { new AuditLog { EntityId = policyId, UserId = agentUserId, Action = "PremiumRenewalReminderSent", CreatedAt = DateTime.UtcNow } });
        _mockCustomerRepo.Setup(r => r.GetByIdAsync(customerId)).ReturnsAsync(new Customer { Id = customerId, UserId = userId });
        _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new User { Id = userId, FirstName = "Bob", LastName = "Jones", Email = "b@j.com", Role = UserRole.Customer, Salutation = Salutation.Mr });

        var result = (await _agentService.GetRenewalRemindersAsync(agentUserId.ToString())).ToList();

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].ReminderSentRecently, Is.True);
    }

    [Test]
    public async Task SendRenewalReminderAsync_AssignedPolicy_CreatesCustomerNotificationAndAudit()
    {
        var agentUserId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var customerUserId = Guid.NewGuid();
        var scheduleId = Guid.NewGuid();

        _mockAgentRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Agent, bool>>>()))
            .ReturnsAsync(new Agent { Id = agentId, UserId = agentUserId });
        _mockPolicyRepo.Setup(r => r.GetByIdAsync(policyId))
            .ReturnsAsync(new Policy { Id = policyId, AgentId = agentId, CustomerId = customerId, PolicyNumber = "POL-100" });
        _mockPremiumScheduleRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<PremiumSchedule, bool>>>()))
            .ReturnsAsync(new[]
            {
                new PremiumSchedule
                {
                    Id = scheduleId,
                    PolicyId = policyId,
                    Amount = 22000m,
                    Status = PremiumScheduleStatus.Upcoming,
                    DueDate = DateTime.UtcNow.AddDays(6)
                }
            });
        _mockCustomerRepo.Setup(r => r.GetByIdAsync(customerId))
            .ReturnsAsync(new Customer { Id = customerId, UserId = customerUserId });
        _mockUserRepo.Setup(r => r.GetByIdAsync(customerUserId))
            .ReturnsAsync(new User { Id = customerUserId, Email = "customer@test.com" });

        await _agentService.SendRenewalReminderAsync(agentUserId.ToString(), policyId.ToString());

        _mockNotifications.Verify(n => n.CreateAsync(
            customerUserId,
            "Premium due soon",
            It.Is<string>(m => m.Contains("POL-100") && m.Contains("22000.00")),
            "payment",
            $"/pay/{policyId}"), Times.Once);
        _mockEmailService.Verify(e => e.SendEmailAsync(
            "customer@test.com",
            It.Is<string>(s => s.Contains("POL-100")),
            It.Is<string>(b => b.Contains("22000.00") && b.Contains($"/pay/{policyId}"))), Times.Once);
        _mockUnitOfWork.Verify(u => u.AuditLogs.AddAsync(It.Is<AuditLog>(a =>
            a.UserId == agentUserId &&
            a.EntityId == policyId &&
            a.Action == "PremiumRenewalReminderSent")), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public void SendRenewalReminderAsync_UnassignedPolicy_ThrowsForbidden()
    {
        var agentUserId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        _mockAgentRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Agent, bool>>>()))
            .ReturnsAsync(new Agent { Id = Guid.NewGuid(), UserId = agentUserId });
        _mockPolicyRepo.Setup(r => r.GetByIdAsync(policyId))
            .ReturnsAsync(new Policy { Id = policyId, AgentId = Guid.NewGuid() });

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ForbiddenException>(() =>
            _agentService.SendRenewalReminderAsync(agentUserId.ToString(), policyId.ToString()));
    }

    [Test]
    public void SendRenewalReminderAsync_RecentlySentReminder_ThrowsConflict()
    {
        var agentUserId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var policyId = Guid.NewGuid();

        _mockAgentRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Agent, bool>>>()))
            .ReturnsAsync(new Agent { Id = agentId, UserId = agentUserId });
        _mockPolicyRepo.Setup(r => r.GetByIdAsync(policyId))
            .ReturnsAsync(new Policy { Id = policyId, AgentId = agentId });
        _mockPremiumScheduleRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<PremiumSchedule, bool>>>()))
            .ReturnsAsync(new[]
            {
                new PremiumSchedule
                {
                    Id = Guid.NewGuid(),
                    PolicyId = policyId,
                    Amount = 22000m,
                    Status = PremiumScheduleStatus.Upcoming,
                    DueDate = DateTime.UtcNow.AddDays(6)
                }
            });
        _mockAuditRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<AuditLog, bool>>>()))
            .ReturnsAsync(new[] { new AuditLog { Action = "PremiumRenewalReminderSent", EntityId = policyId, UserId = agentUserId, CreatedAt = DateTime.UtcNow } });

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ConflictException>(() =>
            _agentService.SendRenewalReminderAsync(agentUserId.ToString(), policyId.ToString()));

        _mockNotifications.Verify(n => n.CreateAsync(
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
        _mockEmailService.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
