using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Moq;
using NUnit.Framework;
using SpeedClaim.Api.Dtos.Grievances;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;
using SpeedClaim.Api.Models.Enums;
using SpeedClaim.Api.Services;

namespace SpeedClaim.Tests.Services;

[TestFixture]
public class GrievanceServiceTests
{
    private Mock<IUnitOfWork> _mockUnitOfWork;
    private Mock<IRepository<Grievance>> _mockGrievanceRepo;
    private Mock<IRepository<Customer>> _mockCustomerRepo;
    private Mock<IPolicyRepository> _mockPolicyRepo;
    private Mock<IClaimRepository> _mockClaimRepo;
    
    private GrievanceService _grievanceService;

    [SetUp]
    public void Setup()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockGrievanceRepo = new Mock<IRepository<Grievance>>();
        _mockCustomerRepo = new Mock<IRepository<Customer>>();
        _mockPolicyRepo = new Mock<IPolicyRepository>();
        _mockClaimRepo = new Mock<IClaimRepository>();

        _mockUnitOfWork.Setup(u => u.Grievances).Returns(_mockGrievanceRepo.Object);
        _mockUnitOfWork.Setup(u => u.Customers).Returns(_mockCustomerRepo.Object);
        _mockUnitOfWork.Setup(u => u.Policies).Returns(_mockPolicyRepo.Object);
        _mockUnitOfWork.Setup(u => u.Claims).Returns(_mockClaimRepo.Object);

        _grievanceService = new GrievanceService(_mockUnitOfWork.Object);
    }

    [Test]
    public async Task RaiseGrievanceAsync_WithValidCustomer_CreatesGrievance()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var customer = new Customer { Id = customerId };
        _mockCustomerRepo.Setup(r => r.GetByIdAsync(customerId)).ReturnsAsync(customer);

        var request = new RaiseGrievanceRequest(null, null, GrievanceCategory.PremiumIssue, "I was charged twice");

        // Act
        var result = await _grievanceService.RaiseGrievanceAsync(customerId, request);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.CustomerId, Is.EqualTo(customerId));
        Assert.That(result.Category, Is.EqualTo(GrievanceCategory.PremiumIssue.ToString()));
        Assert.That(result.Status, Is.EqualTo(GrievanceStatus.Open.ToString()));
        Assert.That(result.Description, Is.EqualTo("I was charged twice"));

        _mockGrievanceRepo.Verify(r => r.AddAsync(It.IsAny<Grievance>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public void RaiseGrievanceAsync_WithInvalidCustomer_ThrowsException()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var request = new RaiseGrievanceRequest(null, null, GrievanceCategory.PremiumIssue, "I was charged twice");

        // Mock returns null for customer
        _mockCustomerRepo.Setup(r => r.GetByIdAsync(customerId)).ReturnsAsync((Customer?)null);

        // Act & Assert
        Assert.ThrowsAsync<KeyNotFoundException>(() => _grievanceService.RaiseGrievanceAsync(customerId, request));
    }

    [Test]
    public async Task UpdateGrievanceStatusAsync_ToResolved_UpdatesStatusAndResolvedAt()
    {
        // Arrange
        var grievanceId = Guid.NewGuid();
        var grievance = new Grievance { Id = grievanceId, Status = GrievanceStatus.InProgress };

        _mockGrievanceRepo.Setup(r => r.GetByIdAsync(grievanceId)).ReturnsAsync(grievance);

        var request = new UpdateGrievanceStatusRequest(GrievanceStatus.Resolved, "Refund processed");

        // Act
        await _grievanceService.UpdateGrievanceStatusAsync(grievanceId, request);

        // Assert
        Assert.That(grievance.Status, Is.EqualTo(GrievanceStatus.Resolved));
        Assert.That(grievance.ResolutionNotes, Is.EqualTo("Refund processed"));
        Assert.That(grievance.ResolvedAt, Is.Not.Null);

        _mockGrievanceRepo.Verify(r => r.Update(grievance), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public async Task AssignGrievanceAsync_AssignsOfficer_AndSetsStatusToInProgress()
    {
        // Arrange
        var grievanceId = Guid.NewGuid();
        var officerId = Guid.NewGuid();
        var grievance = new Grievance { Id = grievanceId, Status = GrievanceStatus.Open };

        _mockGrievanceRepo.Setup(r => r.GetByIdAsync(grievanceId)).ReturnsAsync(grievance);

        // Act
        await _grievanceService.AssignGrievanceAsync(grievanceId, officerId);

        // Assert
        Assert.That(grievance.AssignedToId, Is.EqualTo(officerId));
        Assert.That(grievance.Status, Is.EqualTo(GrievanceStatus.InProgress));

        _mockGrievanceRepo.Verify(r => r.Update(grievance), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public async Task GetMyGrievancesAsync_ReturnsCustomerGrievances()
    {
        var customerId = Guid.NewGuid();
        var grievances = new List<Grievance>
        {
            new Grievance { Id = Guid.NewGuid(), CustomerId = customerId, GrievanceNumber = "GRV-001", Category = GrievanceCategory.PremiumIssue, Description = "Test", Status = GrievanceStatus.Open }
        };
        _mockGrievanceRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Grievance, bool>>>())).ReturnsAsync(grievances);

        var result = await _grievanceService.GetMyGrievancesAsync(customerId);

        Assert.That(result.Count(), Is.EqualTo(1));
        Assert.That(result.First().CustomerId, Is.EqualTo(customerId));
    }

    [Test]
    public async Task GetAllGrievancesAsync_ReturnsAllGrievances()
    {
        var grievances = new List<Grievance>
        {
            new Grievance { Id = Guid.NewGuid(), GrievanceNumber = "GRV-001", Category = GrievanceCategory.ClaimDelay, Description = "D", Status = GrievanceStatus.Open },
            new Grievance { Id = Guid.NewGuid(), GrievanceNumber = "GRV-002", Category = GrievanceCategory.PremiumIssue, Description = "D", Status = GrievanceStatus.InProgress }
        };
        _mockGrievanceRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(grievances);

        var result = await _grievanceService.GetAllGrievancesAsync();

        Assert.That(result.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task GetGrievanceByIdAsync_ValidId_ReturnsGrievance()
    {
        var id = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var grievance = new Grievance { Id = id, CustomerId = customerId, GrievanceNumber = "GRV-001", Category = GrievanceCategory.PremiumIssue, Description = "D", Status = GrievanceStatus.Open };
        _mockGrievanceRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(grievance);

        var result = await _grievanceService.GetGrievanceByIdAsync(id);

        Assert.That(result.Id, Is.EqualTo(id));
    }

    [Test]
    public void GetGrievanceByIdAsync_NotFound_ThrowsKeyNotFound()
    {
        _mockGrievanceRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Grievance?)null);

        Assert.ThrowsAsync<KeyNotFoundException>(() => _grievanceService.GetGrievanceByIdAsync(Guid.NewGuid()));
    }

    [Test]
    public void AssignGrievanceAsync_NotFound_ThrowsKeyNotFound()
    {
        _mockGrievanceRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Grievance?)null);

        Assert.ThrowsAsync<KeyNotFoundException>(() => _grievanceService.AssignGrievanceAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Test]
    public async Task UpdateGrievanceStatusAsync_ToClosed_SetsResolvedAt()
    {
        var grievanceId = Guid.NewGuid();
        var grievance = new Grievance { Id = grievanceId, Status = GrievanceStatus.InProgress };
        _mockGrievanceRepo.Setup(r => r.GetByIdAsync(grievanceId)).ReturnsAsync(grievance);

        await _grievanceService.UpdateGrievanceStatusAsync(grievanceId, new UpdateGrievanceStatusRequest(GrievanceStatus.Closed, null));

        Assert.That(grievance.Status, Is.EqualTo(GrievanceStatus.Closed));
        Assert.That(grievance.ResolvedAt, Is.Not.Null);
    }

    [Test]
    public void UpdateGrievanceStatusAsync_NotFound_ThrowsKeyNotFound()
    {
        _mockGrievanceRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Grievance?)null);

        Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _grievanceService.UpdateGrievanceStatusAsync(Guid.NewGuid(), new UpdateGrievanceStatusRequest(GrievanceStatus.Resolved, null)));
    }

    [Test]
    public async Task RaiseGrievanceAsync_WithValidPolicyId_CreatesGrievance()
    {
        var customerId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        var customer = new Customer { Id = customerId };
        var policy = new Policy { Id = policyId, CustomerId = customerId };

        _mockCustomerRepo.Setup(r => r.GetByIdAsync(customerId)).ReturnsAsync(customer);
        _mockPolicyRepo.Setup(r => r.GetByIdAsync(policyId)).ReturnsAsync(policy);

        var request = new RaiseGrievanceRequest(policyId, null, GrievanceCategory.PolicyServicing, "Wrong coverage");

        var result = await _grievanceService.RaiseGrievanceAsync(customerId, request);

        Assert.That(result.PolicyId, Is.EqualTo(policyId));
        _mockGrievanceRepo.Verify(r => r.AddAsync(It.IsAny<Grievance>()), Times.Once);
    }

    [Test]
    public void RaiseGrievanceAsync_WithInvalidPolicy_ThrowsInvalidOperation()
    {
        var customerId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        var customer = new Customer { Id = customerId };
        var policy = new Policy { Id = policyId, CustomerId = Guid.NewGuid() }; // different customer

        _mockCustomerRepo.Setup(r => r.GetByIdAsync(customerId)).ReturnsAsync(customer);
        _mockPolicyRepo.Setup(r => r.GetByIdAsync(policyId)).ReturnsAsync(policy);

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _grievanceService.RaiseGrievanceAsync(customerId, new RaiseGrievanceRequest(policyId, null, GrievanceCategory.PolicyServicing, "Desc")));
    }

    [Test]
    public void RaiseGrievanceAsync_WithInvalidClaimId_ThrowsInvalidOperation()
    {
        var customerId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        var claimId = Guid.NewGuid();
        var customer = new Customer { Id = customerId };
        var policy = new Policy { Id = policyId, CustomerId = customerId };
        var claim = new Claim { Id = claimId, CustomerId = Guid.NewGuid() }; // different customer

        _mockCustomerRepo.Setup(r => r.GetByIdAsync(customerId)).ReturnsAsync(customer);
        _mockPolicyRepo.Setup(r => r.GetByIdAsync(policyId)).ReturnsAsync(policy);
        _mockUnitOfWork.Setup(u => u.Claims.GetByIdAsync(claimId)).ReturnsAsync(claim);

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _grievanceService.RaiseGrievanceAsync(customerId, new RaiseGrievanceRequest(policyId, claimId, GrievanceCategory.ClaimDelay, "Desc")));
    }

    [Test]
    public async Task RaiseGrievanceAsync_WithValidClaimId_CreatesGrievance()
    {
        var customerId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        var claimId = Guid.NewGuid();
        var customer = new Customer { Id = customerId };
        var policy = new Policy { Id = policyId, CustomerId = customerId };
        var claim = new Claim { Id = claimId, CustomerId = customerId };

        _mockCustomerRepo.Setup(r => r.GetByIdAsync(customerId)).ReturnsAsync(customer);
        _mockPolicyRepo.Setup(r => r.GetByIdAsync(policyId)).ReturnsAsync(policy);
        _mockUnitOfWork.Setup(u => u.Claims.GetByIdAsync(claimId)).ReturnsAsync(claim);

        var result = await _grievanceService.RaiseGrievanceAsync(customerId, new RaiseGrievanceRequest(policyId, claimId, GrievanceCategory.ClaimDelay, "delayed claim"));

        Assert.That(result.ClaimId, Is.EqualTo(claimId));
        _mockGrievanceRepo.Verify(r => r.AddAsync(It.IsAny<Grievance>()), Times.Once);
    }
}
