using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Moq;
using NUnit.Framework;
using SpeedClaim.Api.Dtos.Claims;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;
using SpeedClaim.Api.Models.Enums;
using SpeedClaim.Api.Services;

namespace SpeedClaim.Tests.Services;

[TestFixture]
public class ClaimServiceTests
{
    private Mock<IUnitOfWork> _mockUnitOfWork;
    private Mock<IClaimRepository> _mockClaimRepo;
    private Mock<IPolicyRepository> _mockPolicyRepo;
    private Mock<IRepository<ClaimStatusHistory>> _mockHistoryRepo;
    private Mock<ISubmittedDocumentRepository> _mockDocRepo;
    
    private ClaimService _claimService;

    [SetUp]
    public void Setup()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockClaimRepo = new Mock<IClaimRepository>();
        _mockPolicyRepo = new Mock<IPolicyRepository>();
        _mockHistoryRepo = new Mock<IRepository<ClaimStatusHistory>>();
        _mockDocRepo = new Mock<ISubmittedDocumentRepository>();

        _mockUnitOfWork.Setup(u => u.Claims).Returns(_mockClaimRepo.Object);
        _mockUnitOfWork.Setup(u => u.Policies).Returns(_mockPolicyRepo.Object);
        _mockUnitOfWork.Setup(u => u.ClaimStatusHistories).Returns(_mockHistoryRepo.Object);
        _mockUnitOfWork.Setup(u => u.SubmittedDocuments).Returns(_mockDocRepo.Object);
        _mockUnitOfWork.Setup(u => u.Customers).Returns(new Mock<IRepository<Customer>>().Object);
        _mockUnitOfWork.Setup(u => u.Surveyors).Returns(new Mock<IRepository<Surveyor>>().Object);
        _mockUnitOfWork.Setup(u => u.AuditLogs).Returns(new Mock<IRepository<AuditLog>>().Object);

        _claimService = new ClaimService(_mockUnitOfWork.Object, new Mock<IStorageService>().Object, new Mock<INotificationService>().Object, Mock.Of<Microsoft.Extensions.Logging.ILogger<ClaimService>>());
    }

    [Test]
    public async Task IntimateClaimAsync_WithValidPolicy_CreatesClaim()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        
        var request = new IntimateClaimRequest(
            policyId,
            null,
            ClaimType.Health,
            15000,
            true,
            DateTime.UtcNow.AddDays(-2),
            "Fell and broke arm"
        );

        var policy = new Policy
        {
            Id = policyId,
            CustomerId = customerId,
            Status = PolicyStatus.Active
        };

        _mockPolicyRepo.Setup(r => r.GetByIdAsync(policyId)).ReturnsAsync(policy);

        // Act
        var result = await _claimService.IntimateClaimAsync(customerId, request);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.PolicyId, Is.EqualTo(policyId));
        Assert.That(result.Status, Is.EqualTo(ClaimStatus.Intimated.ToString()));

        _mockClaimRepo.Verify(r => r.AddAsync(It.IsAny<Claim>()), Times.Once);
        _mockHistoryRepo.Verify(r => r.AddAsync(It.IsAny<ClaimStatusHistory>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public void IntimateClaimAsync_WithInactivePolicy_ThrowsException()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        
        var request = new IntimateClaimRequest(policyId, null, ClaimType.Health, 15000, true, DateTime.UtcNow, "Test");

        var policy = new Policy { Id = policyId, CustomerId = customerId, Status = PolicyStatus.Expired };

        _mockPolicyRepo.Setup(r => r.GetByIdAsync(policyId)).ReturnsAsync(policy);

        // Act & Assert
        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.UnprocessableException>(() => _claimService.IntimateClaimAsync(customerId, request));
    }

    [Test]
    public async Task AssignSurveyorAsync_ForMotorClaim_UpdatesSurveyor()
    {
        // Arrange
        var claimId = Guid.NewGuid();
        var surveyorId = Guid.NewGuid();
        var officerId = Guid.NewGuid();
        
        var claim = new Claim
        {
            Id = claimId,
            ClaimType = ClaimType.Accident,
            Status = ClaimStatus.Intimated
        };

        _mockClaimRepo.Setup(r => r.GetByIdAsync(claimId)).ReturnsAsync(claim);

        // Act
        await _claimService.AssignSurveyorAsync(claimId, surveyorId, officerId, "Assigning to John");

        // Assert
        Assert.That(claim.SurveyorId, Is.EqualTo(surveyorId));
        Assert.That(claim.Status, Is.EqualTo(ClaimStatus.UnderReview));
        _mockClaimRepo.Verify(r => r.Update(claim), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public void AssignSurveyorAsync_ForHealthClaim_ThrowsException()
    {
        // Arrange
        var claimId = Guid.NewGuid();
        var claim = new Claim { Id = claimId, ClaimType = ClaimType.Health };

        _mockClaimRepo.Setup(r => r.GetByIdAsync(claimId)).ReturnsAsync(claim);

        // Act & Assert
        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.UnprocessableException>(() => _claimService.AssignSurveyorAsync(claimId, Guid.NewGuid(), Guid.NewGuid(), "Test"));
    }

    [Test]
    public async Task ApproveOrRejectClaimAsync_WhenApproved_UpdatesStatusAndAmount()
    {
        // Arrange
        var claimId = Guid.NewGuid();
        var officerId = Guid.NewGuid();
        var claim = new Claim { Id = claimId, Status = ClaimStatus.UnderReview };

        _mockClaimRepo.Setup(r => r.GetByIdAsync(claimId)).ReturnsAsync(claim);
        var request = new ApproveRejectClaimRequest(true, 10000, "Approved after review");

        // Act
        await _claimService.ApproveOrRejectClaimAsync(claimId, request, officerId);

        // Assert
        Assert.That(claim.Status, Is.EqualTo(ClaimStatus.Approved));
        Assert.That(claim.ClaimAmountApproved, Is.EqualTo(10000));
        _mockClaimRepo.Verify(r => r.Update(claim), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public async Task MarkClaimAsSettledAsync_WhenApproved_UpdatesToSettled()
    {
        // Arrange
        var claimId = Guid.NewGuid();
        var officerId = Guid.NewGuid();
        var claim = new Claim { Id = claimId, Status = ClaimStatus.Approved };

        _mockClaimRepo.Setup(r => r.GetByIdAsync(claimId)).ReturnsAsync(claim);

        // Act
        await _claimService.MarkClaimAsSettledAsync(claimId, officerId);

        // Assert
        Assert.That(claim.Status, Is.EqualTo(ClaimStatus.Settled));
        Assert.That(claim.SettlementDate, Is.Not.Null);
        _mockClaimRepo.Verify(r => r.Update(claim), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public async Task GetClaimByIdAsync_NullCustomerId_ReturnsClaim()
    {
        var claimId = Guid.NewGuid();
        var claim = new Claim { Id = claimId, ClaimNumber = "CLM-001", CustomerId = Guid.NewGuid(), ClaimType = ClaimType.Health, Status = ClaimStatus.Intimated };
        _mockClaimRepo.Setup(r => r.GetByIdAsync(claimId)).ReturnsAsync(claim);

        var result = await _claimService.GetClaimByIdAsync(claimId);

        Assert.That(result.Id, Is.EqualTo(claimId));
    }

    [Test]
    public void GetClaimByIdAsync_NotFound_ThrowsKeyNotFound()
    {
        _mockClaimRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Claim?)null);

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.NotFoundException>(() => _claimService.GetClaimByIdAsync(Guid.NewGuid()));
    }

    [Test]
    public void GetClaimByIdAsync_WrongCustomer_ThrowsUnauthorized()
    {
        var claimId = Guid.NewGuid();
        var claim = new Claim { Id = claimId, CustomerId = Guid.NewGuid(), ClaimType = ClaimType.Health, Status = ClaimStatus.Intimated };
        _mockClaimRepo.Setup(r => r.GetByIdAsync(claimId)).ReturnsAsync(claim);

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ForbiddenException>(() => _claimService.GetClaimByIdAsync(claimId, Guid.NewGuid()));
    }

    [Test]
    public async Task GetClaimHistoryAsync_ValidClaim_ReturnsHistory()
    {
        var claimId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var claim = new Claim { Id = claimId, CustomerId = customerId, ClaimType = ClaimType.Health, Status = ClaimStatus.UnderReview };
        var history = new List<ClaimStatusHistory>
        {
            new ClaimStatusHistory { Id = Guid.NewGuid(), ClaimId = claimId, OldStatus = ClaimStatus.Intimated, NewStatus = ClaimStatus.UnderReview, ChangedAt = DateTimeOffset.UtcNow }
        };
        _mockClaimRepo.Setup(r => r.GetByIdAsync(claimId)).ReturnsAsync(claim);
        _mockHistoryRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ClaimStatusHistory, bool>>>())).ReturnsAsync(history);

        var result = await _claimService.GetClaimHistoryAsync(claimId, customerId);

        Assert.That(result.Count(), Is.EqualTo(1));
    }

    [Test]
    public void GetClaimHistoryAsync_WrongCustomer_ThrowsKeyNotFound()
    {
        var claimId = Guid.NewGuid();
        var claim = new Claim { Id = claimId, CustomerId = Guid.NewGuid(), ClaimType = ClaimType.Health, Status = ClaimStatus.Intimated };
        _mockClaimRepo.Setup(r => r.GetByIdAsync(claimId)).ReturnsAsync(claim);

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.NotFoundException>(() => _claimService.GetClaimHistoryAsync(claimId, Guid.NewGuid()));
    }

    [Test]
    public async Task GetAllClaimsAsync_ReturnsPagedClaims()
    {
        var claims = new List<Claim>
        {
            new Claim { Id = Guid.NewGuid(), ClaimNumber = "CLM-001", ClaimType = ClaimType.Health, Status = ClaimStatus.Intimated },
            new Claim { Id = Guid.NewGuid(), ClaimNumber = "CLM-002", ClaimType = ClaimType.Accident, Status = ClaimStatus.UnderReview }
        };
        _mockClaimRepo.Setup(r => r.GetPagedAsync(1, 20,
                It.IsAny<Expression<Func<Claim, bool>>?>(),
                It.IsAny<Func<IQueryable<Claim>, IQueryable<Claim>>?>()))
            .ReturnsAsync(((IEnumerable<Claim>)claims, claims.Count));

        var result = await _claimService.GetAllClaimsAsync(1, 20);

        Assert.That(result.Data.Count(), Is.EqualTo(2));
        Assert.That(result.TotalRecords, Is.EqualTo(2));
    }

    [Test]
    public async Task UpdateClaimStatusAsync_ValidClaim_UpdatesStatus()
    {
        var claimId = Guid.NewGuid();
        var officerId = Guid.NewGuid();
        var claim = new Claim { Id = claimId, CustomerId = Guid.NewGuid(), ClaimType = ClaimType.Health, Status = ClaimStatus.Intimated };
        _mockClaimRepo.Setup(r => r.GetByIdAsync(claimId)).ReturnsAsync(claim);
        _mockUnitOfWork.Setup(u => u.Customers.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Customer?)null);

        await _claimService.UpdateClaimStatusAsync(claimId, ClaimStatus.UnderReview, officerId, "Reviewing");

        Assert.That(claim.Status, Is.EqualTo(ClaimStatus.UnderReview));
    }

    [Test]
    public async Task RequestAdditionalDocumentsAsync_SetsPendingStatus()
    {
        var claimId = Guid.NewGuid();
        var officerId = Guid.NewGuid();
        var claim = new Claim { Id = claimId, CustomerId = Guid.NewGuid(), ClaimType = ClaimType.Health, Status = ClaimStatus.UnderReview };
        _mockClaimRepo.Setup(r => r.GetByIdAsync(claimId)).ReturnsAsync(claim);
        _mockUnitOfWork.Setup(u => u.Customers.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Customer?)null);

        await _claimService.RequestAdditionalDocumentsAsync(claimId, "Need hospital bill", officerId);

        Assert.That(claim.Status, Is.EqualTo(ClaimStatus.DocumentsPending));
    }

    [Test]
    public async Task ApproveCashlessPreAuthAsync_CashlessClaim_ApprovesPreAuth()
    {
        var claimId = Guid.NewGuid();
        var officerId = Guid.NewGuid();
        var claim = new Claim { Id = claimId, CustomerId = Guid.NewGuid(), ClaimType = ClaimType.Health, Status = ClaimStatus.Intimated, IsCashless = true };
        _mockClaimRepo.Setup(r => r.GetByIdAsync(claimId)).ReturnsAsync(claim);
        _mockUnitOfWork.Setup(u => u.Customers.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Customer?)null);

        await _claimService.ApproveCashlessPreAuthAsync(claimId, officerId);

        Assert.That(claim.Status, Is.EqualTo(ClaimStatus.PreAuthApproved));
    }

    [Test]
    public void ApproveCashlessPreAuthAsync_NonCashlessClaim_ThrowsInvalidOperation()
    {
        var claimId = Guid.NewGuid();
        var claim = new Claim { Id = claimId, ClaimType = ClaimType.Health, Status = ClaimStatus.Intimated, IsCashless = false };
        _mockClaimRepo.Setup(r => r.GetByIdAsync(claimId)).ReturnsAsync(claim);

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.UnprocessableException>(() => _claimService.ApproveCashlessPreAuthAsync(claimId, Guid.NewGuid()));
    }

    [Test]
    public async Task GetAssignedMotorClaimsAsync_ReturnsSurveyorClaims()
    {
        var surveyorId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new Claim { Id = Guid.NewGuid(), SurveyorId = surveyorId, ClaimType = ClaimType.Accident, Status = ClaimStatus.UnderReview }
        };
        _mockClaimRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Claim, bool>>>())).ReturnsAsync(claims);

        var result = await _claimService.GetAssignedMotorClaimsAsync(surveyorId);

        Assert.That(result.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task ApproveOrRejectClaimAsync_Rejected_SetsRejectionReason()
    {
        var claimId = Guid.NewGuid();
        var officerId = Guid.NewGuid();
        var claim = new Claim { Id = claimId, CustomerId = Guid.NewGuid(), ClaimType = ClaimType.Health, Status = ClaimStatus.UnderReview };
        _mockClaimRepo.Setup(r => r.GetByIdAsync(claimId)).ReturnsAsync(claim);
        _mockUnitOfWork.Setup(u => u.Customers.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Customer?)null);

        await _claimService.ApproveOrRejectClaimAsync(claimId, new ApproveRejectClaimRequest(false, null, "Fraudulent claim"), officerId);

        Assert.That(claim.Status, Is.EqualTo(ClaimStatus.Rejected));
        Assert.That(claim.RejectionReason, Is.EqualTo("Fraudulent claim"));
    }

    [Test]
    public void IntimateClaimAsync_PolicyNotFound_ThrowsArgumentException()
    {
        _mockPolicyRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Policy?)null);
        var request = new IntimateClaimRequest(Guid.NewGuid(), null, ClaimType.Health, 1000, false, DateTime.UtcNow, "desc");

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.NotFoundException>(() => _claimService.IntimateClaimAsync(Guid.NewGuid(), request));
    }

    [Test]
    public async Task GetMyClaimsAsync_ReturnsClaims()
    {
        var customerId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new Claim { Id = Guid.NewGuid(), CustomerId = customerId, ClaimNumber = "CLM-001", PolicyId = Guid.NewGuid(), Status = ClaimStatus.Intimated }
        };
        _mockClaimRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Claim, bool>>>())).ReturnsAsync(claims);

        var result = await _claimService.GetMyClaimsAsync(customerId);

        Assert.That(result.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task UploadClaimDocumentAsync_ValidClaim_ReturnsPath()
    {
        var claimId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var claim = new Claim { Id = claimId, CustomerId = customerId, Status = ClaimStatus.UnderReview, ClaimNumber = "CLM-001", PolicyId = Guid.NewGuid() };

        _mockClaimRepo.Setup(r => r.GetByIdAsync(claimId)).ReturnsAsync(claim);
        _mockDocRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<SubmittedDocument, bool>>>()))
            .ReturnsAsync(new List<SubmittedDocument>());

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("doc.pdf");
        mockFile.Setup(f => f.Length).Returns(1024);

        var result = await _claimService.UploadClaimDocumentAsync(claimId, customerId, "medical_bill", mockFile.Object);

        Assert.That(result, Is.Not.Null);
        _mockDocRepo.Verify(r => r.AddAsync(It.IsAny<SubmittedDocument>()), Times.Once);
    }

    [Test]
    public async Task UploadClaimDocumentAsync_IntimatedClaim_AdvancesToUnderReview()
    {
        var claimId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var claim = new Claim { Id = claimId, CustomerId = customerId, Status = ClaimStatus.Intimated, ClaimNumber = "CLM-001", PolicyId = Guid.NewGuid() };

        _mockClaimRepo.Setup(r => r.GetByIdAsync(claimId)).ReturnsAsync(claim);
        _mockDocRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<SubmittedDocument, bool>>>()))
            .ReturnsAsync(new List<SubmittedDocument>());

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("doc.pdf");
        mockFile.Setup(f => f.Length).Returns(1024);

        await _claimService.UploadClaimDocumentAsync(claimId, customerId, "medical_bill", mockFile.Object);

        _mockHistoryRepo.Verify(r => r.AddAsync(It.IsAny<ClaimStatusHistory>()), Times.Once);
    }

    [Test]
    public async Task AssignClaimAsync_ValidClaim_SetsOfficer()
    {
        var claimId = Guid.NewGuid();
        var officerId = Guid.NewGuid();
        var claim = new Claim { Id = claimId, CustomerId = Guid.NewGuid(), Status = ClaimStatus.Intimated, ClaimNumber = "CLM-001", PolicyId = Guid.NewGuid() };

        _mockClaimRepo.Setup(r => r.GetByIdAsync(claimId)).ReturnsAsync(claim);

        await _claimService.AssignClaimAsync(claimId, officerId);

        Assert.That(claim.AssignedOfficerId, Is.EqualTo(officerId));
        _mockHistoryRepo.Verify(r => r.AddAsync(It.IsAny<ClaimStatusHistory>()), Times.Once);
    }

    [Test]
    public async Task SubmitSurveyReportAsync_ValidClaim_UploadsReport()
    {
        var claimId = Guid.NewGuid();
        var surveyorId = Guid.NewGuid();
        var claim = new Claim { Id = claimId, SurveyorId = surveyorId, CustomerId = Guid.NewGuid(), Status = ClaimStatus.UnderReview, ClaimNumber = "CLM-001", PolicyId = Guid.NewGuid() };

        _mockClaimRepo.Setup(r => r.GetByIdAsync(claimId)).ReturnsAsync(claim);
        var mockMotorRepo = new Mock<IRepository<MotorClaimDetail>>();
        mockMotorRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<MotorClaimDetail, bool>>>())).ReturnsAsync((MotorClaimDetail?)null);
        _mockUnitOfWork.Setup(u => u.MotorClaimDetails).Returns(mockMotorRepo.Object);

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("report.pdf");
        mockFile.Setup(f => f.Length).Returns(2048);
        mockFile.Setup(f => f.OpenReadStream()).Returns(new System.IO.MemoryStream());

        var mockStorage = new Mock<IStorageService>();
        mockStorage.Setup(s => s.UploadFileAsync(It.IsAny<System.IO.Stream>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("/storage/report.pdf");
        var svc = new ClaimService(_mockUnitOfWork.Object, mockStorage.Object, new Mock<INotificationService>().Object, Mock.Of<Microsoft.Extensions.Logging.ILogger<ClaimService>>());

        var request = new SubmitSurveyReportRequest(5000m, DateTime.UtcNow.AddDays(-1), "minor damage", mockFile.Object);
        var result = await svc.SubmitSurveyReportAsync(claimId, surveyorId, request);

        Assert.That(result, Is.EqualTo("/storage/report.pdf"));
        _mockDocRepo.Verify(r => r.AddAsync(It.IsAny<SubmittedDocument>()), Times.Once);
    }

    [Test]
    public async Task UpdateClaimStatusInternalAsync_NotifiesCustomer()
    {
        var claimId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var claim = new Claim { Id = claimId, CustomerId = customerId, Status = ClaimStatus.Intimated, ClaimNumber = "CLM-001", PolicyId = Guid.NewGuid() };
        var customer = new Customer { Id = customerId, UserId = userId };

        _mockClaimRepo.Setup(r => r.GetByIdAsync(claimId)).ReturnsAsync(claim);
        _mockUnitOfWork.Setup(u => u.Customers.GetByIdAsync(customerId)).ReturnsAsync(customer);

        var mockNotif = new Mock<INotificationService>();
        var svc = new ClaimService(_mockUnitOfWork.Object, new Mock<IStorageService>().Object, mockNotif.Object, Mock.Of<Microsoft.Extensions.Logging.ILogger<ClaimService>>());

        await svc.UpdateClaimStatusAsync(claimId, ClaimStatus.UnderReview, Guid.NewGuid(), "reviewing");

        mockNotif.Verify(n => n.CreateAsync(userId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Test]
    public void AssignClaimAsync_NotFound_ThrowsNotFoundException()
    {
        _mockClaimRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Claim?)null);

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.NotFoundException>(() =>
            _claimService.AssignClaimAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Test]
    public void UpdateClaimStatusAsync_NotFound_ThrowsNotFoundException()
    {
        _mockClaimRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Claim?)null);

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.NotFoundException>(() =>
            _claimService.UpdateClaimStatusAsync(Guid.NewGuid(), ClaimStatus.UnderReview, Guid.NewGuid(), "notes"));
    }

    [Test]
    public void RequestAdditionalDocumentsAsync_NotFound_ThrowsNotFoundException()
    {
        _mockClaimRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Claim?)null);

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.NotFoundException>(() =>
            _claimService.RequestAdditionalDocumentsAsync(Guid.NewGuid(), "more docs needed", Guid.NewGuid()));
    }

    [Test]
    public void MarkClaimAsSettledAsync_ClaimNotApproved_ThrowsUnprocessableException()
    {
        var claimId = Guid.NewGuid();
        var claim = new Claim { Id = claimId, Status = ClaimStatus.UnderReview, CustomerId = Guid.NewGuid(), ClaimNumber = "CLM-X", PolicyId = Guid.NewGuid() };

        _mockClaimRepo.Setup(r => r.GetByIdAsync(claimId)).ReturnsAsync(claim);

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.UnprocessableException>(() =>
            _claimService.MarkClaimAsSettledAsync(claimId, Guid.NewGuid()));
    }

    [Test]
    public void ApproveOrRejectClaimAsync_ApprovedWithNullAmount_ThrowsValidationException()
    {
        var claimId = Guid.NewGuid();
        var claim = new Claim { Id = claimId, Status = ClaimStatus.UnderReview, CustomerId = Guid.NewGuid(), ClaimNumber = "CLM-X", PolicyId = Guid.NewGuid() };

        _mockClaimRepo.Setup(r => r.GetByIdAsync(claimId)).ReturnsAsync(claim);

        var request = new ApproveRejectClaimRequest(true, null, "approved");

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ValidationException>(() =>
            _claimService.ApproveOrRejectClaimAsync(claimId, request, Guid.NewGuid()));
    }

    [Test]
    public async Task SubmitSurveyReportAsync_WithMotorDetail_UpdatesRepairCost()
    {
        var claimId = Guid.NewGuid();
        var surveyorId = Guid.NewGuid();
        var claim = new Claim { Id = claimId, SurveyorId = surveyorId, CustomerId = Guid.NewGuid(), Status = ClaimStatus.UnderReview, ClaimNumber = "CLM-001", PolicyId = Guid.NewGuid() };
        var motorDetail = new MotorClaimDetail { ClaimId = claimId };

        _mockClaimRepo.Setup(r => r.GetByIdAsync(claimId)).ReturnsAsync(claim);

        var mockMotorRepo = new Mock<IRepository<MotorClaimDetail>>();
        mockMotorRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<MotorClaimDetail, bool>>>())).ReturnsAsync(motorDetail);
        _mockUnitOfWork.Setup(u => u.MotorClaimDetails).Returns(mockMotorRepo.Object);

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("survey.pdf");
        mockFile.Setup(f => f.Length).Returns(1024);
        mockFile.Setup(f => f.OpenReadStream()).Returns(new System.IO.MemoryStream());

        var mockStorage = new Mock<IStorageService>();
        mockStorage.Setup(s => s.UploadFileAsync(It.IsAny<System.IO.Stream>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("/storage/survey.pdf");

        var svc = new ClaimService(_mockUnitOfWork.Object, mockStorage.Object, new Mock<INotificationService>().Object, Mock.Of<Microsoft.Extensions.Logging.ILogger<ClaimService>>());

        var request = new SubmitSurveyReportRequest(12000m, DateTime.UtcNow.AddDays(-2), "heavy damage", mockFile.Object);
        await svc.SubmitSurveyReportAsync(claimId, surveyorId, request);

        Assert.That(motorDetail.EstimatedRepairCost, Is.EqualTo(12000m));
        Assert.That(motorDetail.SurveyorRemarks, Is.EqualTo("heavy damage"));
    }

    [Test]
    public void UploadClaimDocumentAsync_ClaimNotFound_ThrowsNotFoundException()
    {
        var claimId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        _mockClaimRepo.Setup(r => r.GetByIdAsync(claimId)).ReturnsAsync((Claim?)null);

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("doc.pdf");
        mockFile.Setup(f => f.Length).Returns(1024);

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.NotFoundException>(() =>
            _claimService.UploadClaimDocumentAsync(claimId, customerId, "medical_bill", mockFile.Object));
    }

    [Test]
    public void UploadClaimDocumentAsync_NullFile_ThrowsValidationException()
    {
        var claimId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var claim = new Claim { Id = claimId, CustomerId = customerId, Status = ClaimStatus.UnderReview };
        _mockClaimRepo.Setup(r => r.GetByIdAsync(claimId)).ReturnsAsync(claim);

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ValidationException>(() =>
            _claimService.UploadClaimDocumentAsync(claimId, customerId, "medical_bill", null!));
    }
}
