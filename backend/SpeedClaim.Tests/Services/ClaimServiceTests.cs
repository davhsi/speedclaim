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
using System.Linq.Expressions;
using System.Linq;

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
        var user = new User { Id = userId, Email = "test@example.com", FirstName = "Test", LastName = "User", Salutation = "Mr." };
        
        _mockPolicyRepo.Setup(r => r.GetByIdAsync(policyId)).ReturnsAsync(policy);
        _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockDocTypeRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<DocumentType, bool>>>()))
            .ReturnsAsync(new List<DocumentType> { new DocumentType { Code = "DOC1", Name = "Doc", Domain = "HEALTH", IsSensitivePhiPii = true } });

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
        var user = new User { Id = claim.SubmittedById, Email = "customer@example.com", FirstName = "Customer", LastName = "Name", Salutation = "Mr." };

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

    [Test]
    public async Task GetAllClaimsAsync_ReturnsClaims()
    {
        var userId = Guid.NewGuid();
        var claim = new Claim { Id = Guid.NewGuid(), SubmittedById = userId, ClaimNumber = "CLM1" };
        var claims = new List<Claim> { claim };
        _mockClaimRepo.Setup(r => r.GetPagedAsync(1, 10, It.IsAny<Expression<Func<Claim, bool>>>(), It.IsAny<Func<IQueryable<Claim>, IQueryable<Claim>>>()))
            .ReturnsAsync((new List<Claim> { claim }, 1))
            .Callback<int, int, Expression<Func<Claim, bool>>, Func<IQueryable<Claim>, IQueryable<Claim>>>((pn, ps, exp, incl) => {
                // invoke the lambda to cover it
                if (incl != null) {
                    var mockQuery = new List<Claim>().AsQueryable();
                    try { incl(mockQuery); } catch { /* Ignore EF Core async mock issues if they occur on Include */ }
                }
            });

        var result = await _claimService.GetAllClaimsAsync(userId, 1, 10);

        Assert.That(result.Data.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task GetClaimChecklistAsync_ReturnsChecklist()
    {
        var claimId = Guid.NewGuid();
        var checklists = new List<ClaimDocumentChecklist> { new ClaimDocumentChecklist { Id = Guid.NewGuid(), ClaimId = claimId, DocumentTypeCode = "DOC", IsReceived = true } };
        _mockUnitOfWork.Setup(u => u.ClaimDocumentChecklists.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ClaimDocumentChecklist, bool>>>())).ReturnsAsync(checklists);

        var result = await _claimService.GetClaimChecklistAsync(claimId);

        Assert.That(result.Count(), Is.EqualTo(1));
    }

    [Test]
    public void SubmitClaimAsync_PolicyNotFound_ThrowsArgumentException()
    {
        _mockPolicyRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Policy?)null);
        var request = new SubmitClaimRequest { PolicyId = Guid.NewGuid() };
        Assert.ThrowsAsync<ArgumentException>(async () => await _claimService.SubmitClaimAsync(Guid.NewGuid(), request));
    }

    [Test]
    public void SubmitClaimAsync_UnauthorizedUser_ThrowsUnauthorizedAccessException()
    {
        var policy = new HealthPolicy { UserId = Guid.NewGuid() };
        _mockPolicyRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(policy);

        var docTypes = new List<DocumentType> { new DocumentType { Code = "DOC1", Domain = "VEHICLE", Name = "Doc" } };
        _mockUnitOfWork.Setup(u => u.DocumentTypes.FindAsync(It.IsAny<Expression<Func<DocumentType, bool>>>())).ReturnsAsync(docTypes);

        var request = new SubmitClaimRequest { PolicyId = Guid.NewGuid() };
        Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await _claimService.SubmitClaimAsync(Guid.NewGuid(), request));
    }

    [Test]
    public async Task SubmitClaimAsync_HealthDomainWithDetails_CreatesClaim()
    {
        var userId = Guid.NewGuid();
        var policy = new HealthPolicy { Id = Guid.NewGuid(), UserId = userId, Status = "ACTIVE", Domain = "HEALTH", PolicyNumber = "POL123" };
        _mockPolicyRepo.Setup(r => r.GetByIdAsync(policy.Id)).ReturnsAsync(policy);
        _mockDocTypeRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<DocumentType, bool>>>())).ReturnsAsync(new List<DocumentType>());

        var request = new SubmitClaimRequest 
        { 
            PolicyId = policy.Id, 
            HealthDetail = new ClaimHealthDetailDto 
            {
                HospitalName = "Hospital", 
                Diagnosis = "Diagnosis", 
                TreatingDoctor = "Dr", 
                AdmissionDate = DateTime.UtcNow, 
                IsCashless = true
            }
        };

        var result = await _claimService.SubmitClaimAsync(userId, request);
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public async Task SubmitClaimAsync_VehicleDomainWithDetails_CreatesClaim()
    {
        var userId = Guid.NewGuid();
        var policy = new VehiclePolicy { Id = Guid.NewGuid(), UserId = userId, Status = "ACTIVE", Domain = "VEHICLE", PolicyNumber = "POL123" };
        _mockPolicyRepo.Setup(r => r.GetByIdAsync(policy.Id)).ReturnsAsync(policy);
        _mockDocTypeRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<DocumentType, bool>>>())).ReturnsAsync(new List<DocumentType>());

        var request = new SubmitClaimRequest 
        { 
            PolicyId = policy.Id, 
            VehicleDetail = new ClaimVehicleDetailDto 
            {
                AccidentLocation = "Location", 
                FirNumber = "FIR123", 
                RepairEstimate = 5000m, 
                IsTotalLoss = false, 
                SurveyorName = "Surveyor"
            }
        };

        var result = await _claimService.SubmitClaimAsync(userId, request);
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public async Task SubmitClaimAsync_LifeDomainWithDetails_CreatesClaim()
    {
        var userId = Guid.NewGuid();
        var policy = new LifePolicy { Id = Guid.NewGuid(), UserId = userId, Status = "ACTIVE", Domain = "LIFE", PolicyNumber = "POL123" };
        _mockPolicyRepo.Setup(r => r.GetByIdAsync(policy.Id)).ReturnsAsync(policy);
        _mockDocTypeRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<DocumentType, bool>>>())).ReturnsAsync(new List<DocumentType>());

        var request = new SubmitClaimRequest 
        { 
            PolicyId = policy.Id, 
            LifeDetail = new ClaimLifeDetailDto 
            {
                CauseOfDeath = "Cause", 
                PlaceOfDeath = "Place", 
                DeathCertificateNumber = "CERT123", 
                CertifyingDoctor = "Dr", 
                ClaimantName = "Claimant", 
                ClaimantRelation = "Relation"
            }
        };

        var result = await _claimService.SubmitClaimAsync(userId, request);
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public async Task SubmitClaimAsync_WithAttachments_UploadsFiles()
    {
        var userId = Guid.NewGuid();
        var policy = new HealthPolicy { Id = Guid.NewGuid(), UserId = userId, Status = "ACTIVE", Domain = "HEALTH", PolicyNumber = "POL123" };
        _mockPolicyRepo.Setup(r => r.GetByIdAsync(policy.Id)).ReturnsAsync(policy);
        _mockDocTypeRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<DocumentType, bool>>>())).ReturnsAsync(new List<DocumentType>());

        var mockFile = new Mock<Microsoft.AspNetCore.Http.IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("test.pdf");
        mockFile.Setup(f => f.OpenReadStream()).Returns(new System.IO.MemoryStream());

        var request = new SubmitClaimRequest 
        { 
            PolicyId = policy.Id,
            Attachments = new List<Microsoft.AspNetCore.Http.IFormFile> { mockFile.Object }
        };

        _mockStorageService.Setup(s => s.UploadFileAsync(It.IsAny<System.IO.Stream>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("file-path-uuid");

        var result = await _claimService.SubmitClaimAsync(userId, request);
        
        _mockStorageService.Verify(s => s.UploadFileAsync(It.IsAny<System.IO.Stream>(), "test.pdf", userId.ToString()), Times.Once);
        _mockDocRepo.Verify(r => r.AddAsync(It.IsAny<Document>()), Times.Once);
    }

    [Test]
    public void UpdateClaimStatusAsync_ClaimNotFound_ThrowsArgumentException()
    {
        _mockClaimRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Claim?)null);
        var request = new UpdateClaimStatusRequest { Status = "APPROVED" };
        Assert.ThrowsAsync<ArgumentException>(async () => await _claimService.UpdateClaimStatusAsync(Guid.NewGuid(), Guid.NewGuid(), request));
    }

    [Test]
    public void UpdateClaimStatusAsync_InvalidStatus_ThrowsArgumentException()
    {
        var claim = new Claim { Status = "SUBMITTED" };
        _mockClaimRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(claim);
        var request = new UpdateClaimStatusRequest { Status = "INVALID_STATUS" };
        Assert.ThrowsAsync<ArgumentException>(async () => await _claimService.UpdateClaimStatusAsync(Guid.NewGuid(), Guid.NewGuid(), request));
    }
}
