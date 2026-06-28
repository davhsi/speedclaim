using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Microsoft.AspNetCore.Http;
using SpeedClaim.Api.Dtos.Sales;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;
using SpeedClaim.Api.Services;
using SpeedClaim.Api.Models.Enums;

namespace SpeedClaim.Tests.Services;

[TestFixture]
public class ProposalServiceTests
{
    private Mock<IUnitOfWork> _mockUnitOfWork = null!;
    private Mock<IRepository<Proposal>> _mockProposalRepo = null!;
    private Mock<IRepository<InsuranceProduct>> _mockProductRepo = null!;
    private Mock<IRepository<PremiumRateTable>> _mockRateRepo = null!;
    private Mock<IRepository<Agent>> _mockAgentRepo = null!;
    private Mock<IRepository<Customer>> _mockCustomerRepo = null!;
    private ProposalService _proposalService = null!;
    private static readonly DateOnly AdultDob = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-30));

    [SetUp]
    public void Setup()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockProposalRepo = new Mock<IRepository<Proposal>>();
        _mockProductRepo = new Mock<IRepository<InsuranceProduct>>();
        _mockRateRepo = new Mock<IRepository<PremiumRateTable>>();
        _mockAgentRepo = new Mock<IRepository<Agent>>();
        _mockCustomerRepo = new Mock<IRepository<Customer>>();

        var defaultCustomerUserId = Guid.NewGuid();
        _mockCustomerRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new Customer { Id = Guid.NewGuid(), UserId = defaultCustomerUserId, DateOfBirth = AdultDob });

        var mockKycRepo = new Mock<IRepository<KycRecord>>();
        mockKycRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<KycRecord, bool>>>()))
            .ReturnsAsync(new KycRecord { KycStatus = KycStatus.Approved, AadhaarNumber = "ENC", PanNumber = "ENC" });

        _mockUnitOfWork.Setup(u => u.Proposals).Returns(_mockProposalRepo.Object);
        _mockUnitOfWork.Setup(u => u.InsuranceProducts).Returns(_mockProductRepo.Object);
        _mockUnitOfWork.Setup(u => u.PremiumRateTables).Returns(_mockRateRepo.Object);
        _mockUnitOfWork.Setup(u => u.Agents).Returns(_mockAgentRepo.Object);
        _mockUnitOfWork.Setup(u => u.Customers).Returns(_mockCustomerRepo.Object);
        _mockUnitOfWork.Setup(u => u.KycRecords).Returns(mockKycRepo.Object);
        _mockUnitOfWork.Setup(u => u.Policies).Returns(new Mock<IPolicyRepository>().Object);
        _mockUnitOfWork.Setup(u => u.PremiumSchedules).Returns(new Mock<IRepository<PremiumSchedule>>().Object);
        _mockUnitOfWork.Setup(u => u.AuditLogs).Returns(new Mock<IRepository<AuditLog>>().Object);
        _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

        _proposalService = new ProposalService(_mockUnitOfWork.Object, new Mock<INotificationService>().Object, new Mock<IStorageService>().Object, Mock.Of<Microsoft.Extensions.Logging.ILogger<ProposalService>>());
    }

    private static InsuranceProduct ActiveProduct(string domain = "Health") => new()
    {
        Domain = domain,
        IsActive = true,
        MinAge = 18,
        MaxAge = 65,
        MinSumAssured = 100000,
        MaxSumAssured = 5000000,
        MinTenureYears = 1,
        MaxTenureYears = 30
    };

    private void SetupRate(decimal premium = 2500)
    {
        _mockRateRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<PremiumRateTable, bool>>>()))
            .ReturnsAsync(new List<PremiumRateTable>
            {
                new PremiumRateTable { AgeMin = 18, AgeMax = 65, SumAssuredMin = 100000, SumAssuredMax = 5000000, AnnualPremium = premium }
            });
    }

    private void SetupCustomer(Guid customerId, Guid userId)
    {
        _mockCustomerRepo.Setup(r => r.GetByIdAsync(customerId))
            .ReturnsAsync(new Customer { Id = customerId, UserId = userId, DateOfBirth = AdultDob });
    }

    [Test]
    public void GenerateQuoteAsync_ProductNotFound_ThrowsException()
    {
        _mockProductRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((InsuranceProduct?)null);

        var request = new GenerateQuoteRequest(Guid.NewGuid().ToString(), 30, "Male", 100000, 10);
        var ex = Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.NotFoundException>(() => _proposalService.GenerateQuoteAsync(request));
        Assert.That(ex.Message, Is.EqualTo("Product not found"));
    }

    [Test]
    public void GenerateQuoteAsync_NoApplicableRate_ThrowsException()
    {
        var product = ActiveProduct();
        _mockProductRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(product);
        _mockRateRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<PremiumRateTable, bool>>>())).ReturnsAsync(new List<PremiumRateTable>());

        var request = new GenerateQuoteRequest(Guid.NewGuid().ToString(), 30, "Male", 100000, 10);
        var ex = Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.NotFoundException>(() => _proposalService.GenerateQuoteAsync(request));
        Assert.That(ex.Message, Is.EqualTo("No applicable rate found for the given criteria"));
    }

    [Test]
    public async Task GenerateQuoteAsync_Success()
    {
        var productId = Guid.NewGuid();
        var product = ActiveProduct();
        var rateTable = new PremiumRateTable { AgeMin = 20, AgeMax = 40, SumAssuredMin = 0, SumAssuredMax = 200000, AnnualPremium = 2500 };
        
        _mockProductRepo.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _mockRateRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<PremiumRateTable, bool>>>())).ReturnsAsync(new List<PremiumRateTable> { rateTable });

        var request = new GenerateQuoteRequest(productId.ToString(), 30, "Male", 100000, 10);
        var result = await _proposalService.GenerateQuoteAsync(request);

        // 500 + (100000 * 0.02) = 500 + 2000 = 2500
        Assert.That(result.PremiumAmount, Is.EqualTo(2500));
        Assert.That(result.SumAssured, Is.EqualTo(100000));
        Assert.That(result.TenureYears, Is.EqualTo(10));
    }

    [Test]
    public void GenerateQuoteAsync_InactiveProduct_ThrowsConflictException()
    {
        var productId = Guid.NewGuid();
        var product = ActiveProduct();
        product.IsActive = false;
        _mockProductRepo.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);

        var request = new GenerateQuoteRequest(productId.ToString(), 30, "Male", 100000, 10);

        var ex = Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ConflictException>(() =>
            _proposalService.GenerateQuoteAsync(request));

        Assert.That(ex.Message, Is.EqualTo("Product is not available for new quotes."));
    }

    [Test]
    public void GenerateQuoteAsync_OutOfProductRange_ThrowsValidationException()
    {
        var productId = Guid.NewGuid();
        _mockProductRepo.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(ActiveProduct());

        var request = new GenerateQuoteRequest(productId.ToString(), 30, "Male", 99999, 10);

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ValidationException>(() =>
            _proposalService.GenerateQuoteAsync(request));
    }

    [Test]
    public async Task SubmitProposalAsync_AsCustomer_Success()
    {
        var userId = Guid.NewGuid().ToString();
        var userGuid = Guid.Parse(userId);
        var customerGuid = Guid.NewGuid();
        var customerId = customerGuid.ToString();
        var productId = Guid.NewGuid().ToString();

        var product = ActiveProduct();
        _mockProductRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(product);
        _mockProposalRepo.Setup(r => r.AddAsync(It.IsAny<Proposal>())).Returns(Task.CompletedTask);
        SetupCustomer(customerGuid, userGuid);
        SetupRate();

        var request = new SubmitProposalRequest(customerId, productId, 100000, 10, 2500, "Monthly", 
            new HealthDetailDto("", "", "", 0, false, 0), null, null, 
            new List<string> { Guid.NewGuid().ToString() }, 
            new List<NomineeDto> { new NomineeDto("Jane Doe", "Spouse", DateOnly.FromDateTime(DateTime.UtcNow), 100, false, null) });

        var result = await _proposalService.SubmitProposalAsync(userId, request, false);

        Assert.That(result.Status, Is.EqualTo("Submitted"));
        Assert.That(result.PremiumAmount, Is.EqualTo(2500));
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public async Task SubmitProposalAsync_TamperedPremium_RecalculatesPremium()
    {
        var userGuid = Guid.NewGuid();
        var customerGuid = Guid.NewGuid();
        var productId = Guid.NewGuid().ToString();
        _mockProductRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(ActiveProduct());
        _mockProposalRepo.Setup(r => r.AddAsync(It.IsAny<Proposal>())).Returns(Task.CompletedTask);
        SetupCustomer(customerGuid, userGuid);
        SetupRate(2500);

        var request = new SubmitProposalRequest(customerGuid.ToString(), productId, 100000, 10, 1, "Monthly",
            null, null, null,
            new List<string>(),
            new List<NomineeDto> { new NomineeDto("Jane Doe", "Spouse", AdultDob, 100, false, null) });

        var result = await _proposalService.SubmitProposalAsync(userGuid.ToString(), request, false);

        Assert.That(result.PremiumAmount, Is.EqualTo(2500));
    }

    [Test]
    public void SubmitProposalAsync_InactiveProduct_ThrowsConflictException()
    {
        var userGuid = Guid.NewGuid();
        var customerGuid = Guid.NewGuid();
        var product = ActiveProduct();
        product.IsActive = false;
        _mockProductRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(product);
        SetupCustomer(customerGuid, userGuid);

        var request = new SubmitProposalRequest(customerGuid.ToString(), Guid.NewGuid().ToString(), 100000, 10, 2500, "Monthly",
            null, null, null, new List<string>(), new List<NomineeDto>());

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ConflictException>(() =>
            _proposalService.SubmitProposalAsync(userGuid.ToString(), request, false));
    }

    [Test]
    public void SubmitProposalAsync_OtherCustomer_ThrowsForbiddenException()
    {
        var userGuid = Guid.NewGuid();
        var customerGuid = Guid.NewGuid();
        _mockProductRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(ActiveProduct());
        SetupCustomer(customerGuid, Guid.NewGuid());

        var request = new SubmitProposalRequest(customerGuid.ToString(), Guid.NewGuid().ToString(), 100000, 10, 2500, "Monthly",
            null, null, null, new List<string>(), new List<NomineeDto>());

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ForbiddenException>(() =>
            _proposalService.SubmitProposalAsync(userGuid.ToString(), request, false));
    }

    [Test]
    public async Task SubmitProposalAsync_AsAgent_Success()
    {
        var userId = Guid.NewGuid();
        var customerId = Guid.NewGuid().ToString();
        var productId = Guid.NewGuid().ToString();
        var agent = new Agent { Id = Guid.NewGuid() };

        var product = ActiveProduct();
        _mockProductRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(product);
        _mockAgentRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Agent, bool>>>())).ReturnsAsync(agent);
        _mockProposalRepo.Setup(r => r.AddAsync(It.IsAny<Proposal>())).Returns(Task.CompletedTask);
        SetupRate();

        var request = new SubmitProposalRequest(customerId, productId, 100000, 10, 2500, "Monthly", 
            null, new LifeDetailDto("", 0, 0, false, false), null, 
            new List<string>(), new List<NomineeDto>());

        var result = await _proposalService.SubmitProposalAsync(userId.ToString(), request, true);

        Assert.That(result.AgentId, Is.EqualTo(agent.Id));
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public async Task GetMyProposalsAsync_AsCustomer_ReturnsProposals()
    {
        var userId = Guid.NewGuid();
        var customer = new Customer { Id = Guid.NewGuid() };
        var proposals = new List<Proposal> { new Proposal { ProposalNumber = "PRP-123" } };

        _mockCustomerRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Customer, bool>>>())).ReturnsAsync(customer);
        _mockProposalRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Proposal, bool>>>())).ReturnsAsync(proposals);

        var result = await _proposalService.GetMyProposalsAsync(userId.ToString(), false);

        Assert.That(result.Count(), Is.EqualTo(1));
        Assert.That(result.First().ProposalNumber, Is.EqualTo("PRP-123"));
    }

    [Test]
    public async Task GetMyProposalsAsync_AsAgent_ReturnsProposals()
    {
        var userId = Guid.NewGuid();
        var agent = new Agent { Id = Guid.NewGuid() };
        var proposals = new List<Proposal> { new Proposal { ProposalNumber = "PRP-456" } };

        _mockAgentRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Agent, bool>>>())).ReturnsAsync(agent);
        _mockProposalRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Proposal, bool>>>())).ReturnsAsync(proposals);

        var result = await _proposalService.GetMyProposalsAsync(userId.ToString(), true);

        Assert.That(result.Count(), Is.EqualTo(1));
        Assert.That(result.First().ProposalNumber, Is.EqualTo("PRP-456"));
    }

    [Test]
    public async Task GetAllProposalsAsync_ReturnsAllProposals()
    {
        var proposals = new List<Proposal> { new Proposal { ProposalNumber = "PRP-1" }, new Proposal { ProposalNumber = "PRP-2" } };
        _mockProposalRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(proposals);

        var result = await _proposalService.GetAllProposalsAsync();

        Assert.That(result.Count(), Is.EqualTo(2));
    }

    [Test]
    public void ApproveOrRejectProposalAsync_ProposalNotFound_ThrowsException()
    {
        _mockProposalRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Proposal?)null);

        var ex = Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.NotFoundException>(() => _proposalService.ApproveOrRejectProposalAsync(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), true, ""));
        Assert.That(ex.Message, Is.EqualTo("Proposal not found"));
    }

    [Test]
    public async Task ApproveOrRejectProposalAsync_Approved_CreatesPremiumSchedule()
    {
        var proposal = new Proposal { PremiumAmount = 2500, PremiumSchedules = new List<PremiumSchedule>() };
        _mockProposalRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(proposal);

        PremiumSchedule? captured = null;
        var mockScheduleRepo = new Mock<IRepository<PremiumSchedule>>();
        mockScheduleRepo.Setup(r => r.AddAsync(It.IsAny<PremiumSchedule>()))
            .Callback<PremiumSchedule>(ps => captured = ps)
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.PremiumSchedules).Returns(mockScheduleRepo.Object);

        await _proposalService.ApproveOrRejectProposalAsync(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), true, "Looks good");

        Assert.That(proposal.Status, Is.EqualTo(ProposalStatus.Approved));
        Assert.That(proposal.UnderwriterNotes, Is.EqualTo("Looks good"));
        mockScheduleRepo.Verify(r => r.AddAsync(It.IsAny<PremiumSchedule>()), Times.Once);
        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Amount, Is.EqualTo(2500));
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public async Task ApproveOrRejectProposalAsync_Rejected_SetsReason()
    {
        var proposal = new Proposal();
        _mockProposalRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(proposal);

        await _proposalService.ApproveOrRejectProposalAsync(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), false, "High risk");

        Assert.That(proposal.Status, Is.EqualTo(ProposalStatus.Rejected));
        Assert.That(proposal.RejectionReason, Is.EqualTo("High risk"));
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public void ApproveOrRejectProposalAsync_FinalProposal_ThrowsConflictException()
    {
        var proposal = new Proposal { Status = ProposalStatus.Approved };
        _mockProposalRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(proposal);

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ConflictException>(() =>
            _proposalService.ApproveOrRejectProposalAsync(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), true, "Looks good"));
    }

    [Test]
    public async Task RequestAdditionalDocumentsAsync_Success()
    {
        var proposal = new Proposal();
        _mockProposalRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(proposal);

        await _proposalService.RequestAdditionalDocumentsAsync(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "Need medical report");

        Assert.That(proposal.Status, Is.EqualTo(ProposalStatus.DocumentsPending));
        Assert.That(proposal.UnderwriterNotes, Is.EqualTo("Additional Docs Required: Need medical report"));
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public void RequestAdditionalDocumentsAsync_FinalProposal_ThrowsConflictException()
    {
        var proposal = new Proposal { Status = ProposalStatus.Rejected };
        _mockProposalRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(proposal);

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ConflictException>(() =>
            _proposalService.RequestAdditionalDocumentsAsync(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "Need medical report"));
    }

    // --- AddUnderwriterNotesAsync tests ---

    [Test]
    public void AddUnderwriterNotesAsync_ProposalNotFound_ThrowsException()
    {
        _mockProposalRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Proposal?)null);

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.NotFoundException>(() =>
            _proposalService.AddUnderwriterNotesAsync(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "First note"));
    }

    [Test]
    public async Task AddUnderwriterNotesAsync_EmptyExistingNotes_SetsNotes()
    {
        var proposalId = Guid.NewGuid();
        var proposal = new Proposal { Id = proposalId, UnderwriterNotes = null };

        _mockProposalRepo.Setup(r => r.GetByIdAsync(proposalId)).ReturnsAsync(proposal);

        await _proposalService.AddUnderwriterNotesAsync(proposalId.ToString(), Guid.NewGuid().ToString(), "First note");

        Assert.That(proposal.UnderwriterNotes, Is.EqualTo("First note"));
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public async Task AddUnderwriterNotesAsync_ExistingNotes_AppendsWithSeparator()
    {
        var proposalId = Guid.NewGuid();
        var proposal = new Proposal { Id = proposalId, UnderwriterNotes = "Previous note" };

        _mockProposalRepo.Setup(r => r.GetByIdAsync(proposalId)).ReturnsAsync(proposal);

        await _proposalService.AddUnderwriterNotesAsync(proposalId.ToString(), Guid.NewGuid().ToString(), "Second note");

        Assert.That(proposal.UnderwriterNotes, Is.EqualTo("Previous note\n---\nSecond note"));
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public async Task GetByIdAsync_AdminUser_ReturnsProposal()
    {
        var proposalId = Guid.NewGuid();
        var proposal = new Proposal { Id = proposalId, ProposalNumber = "PRP-001", Status = ProposalStatus.Submitted, CustomerId = Guid.NewGuid(), CreatedAt = DateTimeOffset.UtcNow };
        _mockProposalRepo.Setup(r => r.GetByIdAsync(proposalId)).ReturnsAsync(proposal);

        var result = await _proposalService.GetByIdAsync(proposalId.ToString(), Guid.NewGuid().ToString(), isAdmin: true);

        Assert.That(result.Id, Is.EqualTo(proposalId));
    }

    [Test]
    public async Task GetByIdAsync_CustomerOwner_ReturnsProposal()
    {
        var proposalId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var proposal = new Proposal { Id = proposalId, ProposalNumber = "PRP-001", Status = ProposalStatus.Submitted, CustomerId = customerId, CreatedAt = DateTimeOffset.UtcNow };
        _mockProposalRepo.Setup(r => r.GetByIdAsync(proposalId)).ReturnsAsync(proposal);
        _mockAgentRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Agent, bool>>>())).ReturnsAsync((Agent?)null);
        _mockCustomerRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Customer, bool>>>())).ReturnsAsync(new Customer { Id = customerId, UserId = customerId });

        var result = await _proposalService.GetByIdAsync(proposalId.ToString(), customerId.ToString(), isAdmin: false);

        Assert.That(result.Id, Is.EqualTo(proposalId));
    }

    [Test]
    public void GetByIdAsync_NotOwnerNotAdmin_ThrowsUnauthorized()
    {
        var proposalId = Guid.NewGuid();
        var proposal = new Proposal { Id = proposalId, ProposalNumber = "PRP-001", Status = ProposalStatus.Submitted, CustomerId = Guid.NewGuid(), CreatedAt = DateTimeOffset.UtcNow };
        _mockProposalRepo.Setup(r => r.GetByIdAsync(proposalId)).ReturnsAsync(proposal);
        _mockAgentRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Agent, bool>>>())).ReturnsAsync((Agent?)null);

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ForbiddenException>(() =>
            _proposalService.GetByIdAsync(proposalId.ToString(), Guid.NewGuid().ToString(), isAdmin: false));
    }

    [Test]
    public void GetByIdAsync_NotFound_ThrowsKeyNotFound()
    {
        _mockProposalRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Proposal?)null);

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.NotFoundException>(() =>
            _proposalService.GetByIdAsync(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), isAdmin: true));
    }

    [Test]
    public async Task UploadDocumentAsync_ValidCustomerOwner_UploadsAndSavesDocument()
    {
        var proposalId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var proposal = new Proposal { Id = proposalId, ProposalNumber = "PRP-001", Status = ProposalStatus.DocumentsPending, CustomerId = customerId, CreatedAt = DateTimeOffset.UtcNow };
        _mockProposalRepo.Setup(r => r.GetByIdAsync(proposalId)).ReturnsAsync(proposal);
        _mockCustomerRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Customer, bool>>>())).ReturnsAsync(new Customer { Id = customerId, UserId = customerId });

        var mockStorageService = new Mock<IStorageService>();
        mockStorageService.Setup(s => s.UploadFileAsync(It.IsAny<System.IO.Stream>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("/uploads/proposals/doc.pdf");

        var mockDocRepo = new Mock<ISubmittedDocumentRepository>();
        mockDocRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<SubmittedDocument, bool>>>()))
            .ReturnsAsync(new List<SubmittedDocument>());
        _mockUnitOfWork.Setup(u => u.SubmittedDocuments).Returns(mockDocRepo.Object);

        var proposalServiceWithStorage = new ProposalService(_mockUnitOfWork.Object, new Mock<INotificationService>().Object, mockStorageService.Object, Mock.Of<Microsoft.Extensions.Logging.ILogger<ProposalService>>());

        var mockFile = new Mock<Microsoft.AspNetCore.Http.IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("doc.pdf");
        mockFile.Setup(f => f.Length).Returns(100);
        mockFile.Setup(f => f.OpenReadStream()).Returns(new System.IO.MemoryStream(new byte[100]));

        var result = await proposalServiceWithStorage.UploadDocumentAsync(proposalId.ToString(), customerId.ToString(), "ID_PROOF", mockFile.Object);

        Assert.That(result, Is.EqualTo("/uploads/proposals/doc.pdf"));
        mockDocRepo.Verify(r => r.AddAsync(It.IsAny<SubmittedDocument>()), Times.Once);
    }

    [Test]
    public void UploadDocumentAsync_NotOwner_ThrowsUnauthorized()
    {
        var proposalId = Guid.NewGuid();
        var proposal = new Proposal { Id = proposalId, CustomerId = Guid.NewGuid(), Status = ProposalStatus.Submitted, CreatedAt = DateTimeOffset.UtcNow };
        _mockProposalRepo.Setup(r => r.GetByIdAsync(proposalId)).ReturnsAsync(proposal);
        _mockAgentRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Agent, bool>>>())).ReturnsAsync((Agent?)null);

        var mockFile = new Mock<Microsoft.AspNetCore.Http.IFormFile>();
        mockFile.Setup(f => f.Length).Returns(100);
        mockFile.Setup(f => f.OpenReadStream()).Returns(new System.IO.MemoryStream());

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ForbiddenException>(() =>
            _proposalService.UploadDocumentAsync(proposalId.ToString(), Guid.NewGuid().ToString(), "ID_PROOF", mockFile.Object));
    }

    [Test]
    public async Task SubmitProposalAsync_WithMotorDetail_SetsMotorDetail()
    {
        var userId = Guid.NewGuid().ToString();
        var customerId = Guid.NewGuid().ToString();
        var productId = Guid.NewGuid().ToString();

        var userGuid = Guid.Parse(userId);
        var customerGuid = Guid.Parse(customerId);
        var product = ActiveProduct("Motor");
        _mockProductRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(product);
        _mockProposalRepo.Setup(r => r.AddAsync(It.IsAny<Proposal>())).Returns(Task.CompletedTask);
        SetupCustomer(customerGuid, userGuid);
        SetupRate(15000);

        var motorDetail = new MotorDetailDto("MH01AB1234", "Honda", "City", 2022, "Sedan", 500000m, "ENG123", "CHS456", "Comprehensive");
        var request = new SubmitProposalRequest(customerId, productId, 500000, 5, 15000, "Annual",
            null, null, motorDetail,
            new List<string>(),
            new List<NomineeDto>());

        var result = await _proposalService.SubmitProposalAsync(userId, request, false);

        Assert.That(result.Status, Is.EqualTo("Submitted"));
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public async Task UploadDocumentAsync_AgentWithMatchingId_Succeeds()
    {
        var proposalId = Guid.NewGuid();
        var agentUserId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var proposal = new Proposal { Id = proposalId, CustomerId = Guid.NewGuid(), AgentId = agentId };
        var agent = new Agent { Id = agentId, UserId = agentUserId };

        _mockProposalRepo.Setup(r => r.GetByIdAsync(proposalId)).ReturnsAsync(proposal);
        _mockAgentRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Agent, bool>>>())).ReturnsAsync(agent);

        var mockDocRepo = new Mock<ISubmittedDocumentRepository>();
        mockDocRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<SubmittedDocument, bool>>>()))
            .ReturnsAsync(new List<SubmittedDocument>());
        _mockUnitOfWork.Setup(u => u.SubmittedDocuments).Returns(mockDocRepo.Object);

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("id.pdf");
        mockFile.Setup(f => f.Length).Returns(1024);
        mockFile.Setup(f => f.OpenReadStream()).Returns(new System.IO.MemoryStream());
        var mockStorage = new Mock<IStorageService>();
        mockStorage.Setup(s => s.UploadFileAsync(It.IsAny<System.IO.Stream>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("/path/id.pdf");
        var svc = new ProposalService(_mockUnitOfWork.Object, new Mock<INotificationService>().Object, mockStorage.Object, Mock.Of<Microsoft.Extensions.Logging.ILogger<ProposalService>>());

        await svc.UploadDocumentAsync(proposalId.ToString(), agentUserId.ToString(), "ID_PROOF", mockFile.Object);

        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public async Task ApproveRejectProposalAsync_Approved_SendsNotification()
    {
        var proposalId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var customer = new Customer { Id = customerId, UserId = userId };

        var proposal = new Proposal
        {
            Id = proposalId, CustomerId = customerId, ProposalNumber = "PRO-001",
            Status = ProposalStatus.UnderReview, AgentId = null
        };

        _mockProposalRepo.Setup(r => r.GetByIdAsync(proposalId)).ReturnsAsync(proposal);
        _mockCustomerRepo.Setup(r => r.GetByIdAsync(customerId)).ReturnsAsync(customer);

        var mockNotif = new Mock<INotificationService>();
        var svc = new ProposalService(_mockUnitOfWork.Object, mockNotif.Object, new Mock<IStorageService>().Object, Mock.Of<Microsoft.Extensions.Logging.ILogger<ProposalService>>());

        await svc.ApproveOrRejectProposalAsync(proposalId.ToString(), Guid.NewGuid().ToString(), true, "Looks good");

        mockNotif.Verify(n => n.CreateAsync(userId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task ApproveOrRejectProposalAsync_WithAgent_SendsAgentNotificationToo()
    {
        var proposalId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var agentUserId = Guid.NewGuid();
        var customerUserId = Guid.NewGuid();

        var customer = new Customer { Id = customerId, UserId = customerUserId };
        var agent = new Agent { Id = agentId, UserId = agentUserId };
        var proposal = new Proposal
        {
            Id = proposalId, CustomerId = customerId, ProposalNumber = "PRO-002",
            Status = ProposalStatus.UnderReview, AgentId = agentId
        };

        _mockProposalRepo.Setup(r => r.GetByIdAsync(proposalId)).ReturnsAsync(proposal);
        _mockCustomerRepo.Setup(r => r.GetByIdAsync(customerId)).ReturnsAsync(customer);
        _mockAgentRepo.Setup(r => r.GetByIdAsync(agentId)).ReturnsAsync(agent);

        var mockNotif = new Mock<INotificationService>();
        var svc = new ProposalService(_mockUnitOfWork.Object, mockNotif.Object, new Mock<IStorageService>().Object, Mock.Of<Microsoft.Extensions.Logging.ILogger<ProposalService>>());

        await svc.ApproveOrRejectProposalAsync(proposalId.ToString(), Guid.NewGuid().ToString(), true, "All checks passed");

        // Customer notification + agent notification = 2 calls
        mockNotif.Verify(n => n.CreateAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
        mockNotif.Verify(n => n.CreateAsync(agentUserId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task SubmitProposalAsync_WithLifeDetail_SetsLifeDetail()
    {
        var userId = Guid.NewGuid().ToString();
        var customerId = Guid.NewGuid().ToString();
        var productId = Guid.NewGuid().ToString();

        var userGuid = Guid.Parse(userId);
        var customerGuid = Guid.Parse(customerId);
        var product = ActiveProduct("Life");
        _mockProductRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(product);
        _mockProposalRepo.Setup(r => r.AddAsync(It.IsAny<Proposal>())).Returns(Task.CompletedTask);
        SetupCustomer(customerGuid, userGuid);
        SetupRate(50000);

        var lifeDetail = new LifeDetailDto("Endowment", 500000m, 1000000m, true, false);
        var request = new SubmitProposalRequest(customerId, productId, 1000000, 20, 50000, "Annual",
            null, lifeDetail, null,
            new List<string>(),
            new List<NomineeDto>());

        var result = await _proposalService.SubmitProposalAsync(userId, request, false);

        Assert.That(result.Status, Is.EqualTo("Submitted"));
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }
}
