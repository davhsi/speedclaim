using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Moq;
using SpeedClaim.Api.Dtos.Claims;
using SpeedClaim.Api.Models;
using SpeedClaim.Api.Services;
using SpeedClaim.Api.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpeedClaim.Tests.Services;

[TestFixture]
public class ClaimServiceTests
{
    private Mock<IUnitOfWork> _mockUnitOfWork;
    private Mock<IClaimRepository> _mockClaimRepo;
    private Mock<IPolicyRepository> _mockPolicyRepo;
    private Mock<IRepository<ClaimWorkflow>> _mockWorkflowRepo;
    private Mock<IDocumentRepository> _mockDocRepo;
    private Mock<IUserRepository> _mockUserRepo;
    private Mock<IStorageService> _mockStorageService;
    private Mock<IEmailService> _mockEmailService;
    private Mock<IRepository<DocumentType>> _mockDocTypeRepo;
    private ClaimService _claimService;

    [SetUp]
    public void Setup()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockClaimRepo = new Mock<IClaimRepository>();
        _mockPolicyRepo = new Mock<IPolicyRepository>();
        _mockWorkflowRepo = new Mock<IRepository<ClaimWorkflow>>();
        _mockDocRepo = new Mock<IDocumentRepository>();
        _mockUserRepo = new Mock<IUserRepository>();

        _mockUnitOfWork.Setup(u => u.Claims).Returns(_mockClaimRepo.Object);
        _mockUnitOfWork.Setup(u => u.Policies).Returns(_mockPolicyRepo.Object);
        _mockUnitOfWork.Setup(u => u.ClaimWorkflows).Returns(_mockWorkflowRepo.Object);
        _mockUnitOfWork.Setup(u => u.Documents).Returns(_mockDocRepo.Object);
        _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepo.Object);

        _mockDocTypeRepo = new Mock<IRepository<DocumentType>>();
        _mockUnitOfWork.Setup(u => u.DocumentTypes).Returns(_mockDocTypeRepo.Object);

        _mockStorageService = new Mock<IStorageService>();
        _mockEmailService = new Mock<IEmailService>();

        _claimService = new ClaimService(_mockUnitOfWork.Object, _mockStorageService.Object, _mockEmailService.Object);
    }

    [Test]
    public async Task SubmitClaimAsync_ValidRequest_CreatesClaimAndWorkflow()
    {
        var userId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        var policy = new HealthPolicy { Id = policyId, UserId = userId, Status = "ACTIVE", Domain = "HEALTH", PolicyNumber = "POL123" };
        var user = new User { Id = userId, Email = "test@example.com", FullName = "Test" };
        
        _mockPolicyRepo.Setup(r => r.GetByIdAsync(policyId)).ReturnsAsync(policy);
        _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockDocTypeRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<DocumentType, bool>>>()))
            .ReturnsAsync(new List<DocumentType>());

        var request = new SubmitClaimRequest 
        { 
            PolicyId = policyId, 
            AmountRequested = 5000m, 
            Description = "Accident", 
            IncidentDate = DateTime.UtcNow.AddDays(-1) 
        };

        var result = await _claimService.SubmitClaimAsync(userId, request);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Status, Is.EqualTo("SUBMITTED"));
        _mockClaimRepo.Verify(r => r.AddAsync(It.IsAny<Claim>()), Times.Once);
        _mockWorkflowRepo.Verify(r => r.AddAsync(It.IsAny<ClaimWorkflow>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Exactly(2));
    }

    [Test]
    public void SubmitClaimAsync_PolicyNotActive_ThrowsArgumentException()
    {
        var userId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        var policy = new HealthPolicy { Id = policyId, UserId = userId, Status = "PENDING", Domain = "HEALTH" };
        
        _mockPolicyRepo.Setup(r => r.GetByIdAsync(policyId)).ReturnsAsync(policy);

        var request = new SubmitClaimRequest 
        { 
            PolicyId = policyId, 
            AmountRequested = 5000m, 
            Description = "Accident", 
            IncidentDate = DateTime.UtcNow 
        };

        var ex = Assert.ThrowsAsync<ArgumentException>(async () => await _claimService.SubmitClaimAsync(userId, request));
        Assert.That(ex.Message, Does.Contain("status: PENDING"));
    }

    [Test]
    public async Task UpdateClaimStatusAsync_ValidStatus_UpdatesStatusAndCreatesWorkflow()
    {
        var claimId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var claim = new Claim { Id = claimId, Status = "SUBMITTED", SubmittedById = Guid.NewGuid(), ClaimNumber = "CLM1" };
        var user = new User { Id = claim.SubmittedById, Email = "customer@example.com", FullName = "Customer" };

        _mockClaimRepo.Setup(r => r.GetByIdAsync(claimId)).ReturnsAsync(claim);
        _mockUserRepo.Setup(r => r.GetByIdAsync(claim.SubmittedById)).ReturnsAsync(user);

        var request = new UpdateClaimStatusRequest 
        { 
            Status = "APPROVED", 
            Remarks = "Looks good", 
            ApprovedAmount = 4500m 
        };

        var result = await _claimService.UpdateClaimStatusAsync(claimId, actorId, request);

        Assert.That(result.Status, Is.EqualTo("APPROVED"));
        Assert.That(claim.ApprovedAmount, Is.EqualTo(4500m));
        _mockWorkflowRepo.Verify(r => r.AddAsync(It.IsAny<ClaimWorkflow>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }
}
