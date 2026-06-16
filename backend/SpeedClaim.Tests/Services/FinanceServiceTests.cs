using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using SpeedClaim.Api.Dtos.Financial;
using SpeedClaim.Api.Dtos.Payments;

using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;
using SpeedClaim.Api.Models.Enums;
using SpeedClaim.Api.Services;
using Stripe;

namespace SpeedClaim.Tests.Services;

[TestFixture]
public class FinanceServiceTests
{
    private Mock<IUnitOfWork> _mockUnitOfWork;
    private Mock<IStripeWrapper> _mockStripeWrapper;
    private Mock<IConfiguration> _mockConfig;
    private Mock<IEmailService> _mockEmailService;
    private FinanceService _financeService;

    [SetUp]
    public void Setup()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockStripeWrapper = new Mock<IStripeWrapper>();
        _mockConfig = new Mock<IConfiguration>();
        _mockEmailService = new Mock<IEmailService>();

        _mockUnitOfWork.Setup(u => u.PremiumSchedules).Returns(new Mock<IRepository<PremiumSchedule>>().Object);
        _mockUnitOfWork.Setup(u => u.PremiumPayments).Returns(new Mock<IPremiumPaymentRepository>().Object);
        _mockUnitOfWork.Setup(u => u.AgentCommissions).Returns(new Mock<IRepository<AgentCommission>>().Object);
        _mockUnitOfWork.Setup(u => u.Customers).Returns(new Mock<IRepository<SpeedClaim.Api.Models.Customer>>().Object);
        _mockUnitOfWork.Setup(u => u.StripeCustomers).Returns(new Mock<IRepository<SpeedClaim.Api.Models.StripeCustomer>>().Object);
        _mockUnitOfWork.Setup(u => u.Users).Returns(new Mock<IUserRepository>().Object);

        _financeService = new FinanceService(_mockUnitOfWork.Object, _mockStripeWrapper.Object, _mockConfig.Object, _mockEmailService.Object, new Mock<INotificationService>().Object, Mock.Of<Microsoft.Extensions.Logging.ILogger<FinanceService>>());
    }

    [Test]
    public async Task PayPremiumAsync_ValidRequest_ReturnsResponse()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var scheduleId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        var request = new CreatePaymentIntentRequest { PolicyId = policyId };

        var schedule = new PremiumSchedule
        {
            Id = scheduleId,
            PolicyId = policyId,
            Amount = 150.0m,
            Status = PremiumScheduleStatus.Upcoming,
            InstallmentNumber = 1
        };

        var mockScheduleRepo = new Mock<IRepository<PremiumSchedule>>();
        mockScheduleRepo.Setup(r => r.GetByIdAsync(scheduleId)).ReturnsAsync(schedule);
        _mockUnitOfWork.Setup(u => u.PremiumSchedules).Returns(mockScheduleRepo.Object);

        var customerEntity = new SpeedClaim.Api.Models.Customer { Id = customerId, UserId = Guid.NewGuid() };
        var mockCustomerRepo = new Mock<IRepository<SpeedClaim.Api.Models.Customer>>();
        mockCustomerRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<SpeedClaim.Api.Models.Customer, bool>>>())).ReturnsAsync(customerEntity);
        _mockUnitOfWork.Setup(u => u.Customers).Returns(mockCustomerRepo.Object);

        var existingStripeCustomer = new SpeedClaim.Api.Models.StripeCustomer { Id = Guid.NewGuid(), UserId = customerEntity.UserId, StripeCustomerId = "cus_test" };
        var mockStripeCustomerRepo = new Mock<IRepository<SpeedClaim.Api.Models.StripeCustomer>>();
        mockStripeCustomerRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<SpeedClaim.Api.Models.StripeCustomer, bool>>>()))
            .ReturnsAsync(existingStripeCustomer);
        _mockUnitOfWork.Setup(u => u.StripeCustomers).Returns(mockStripeCustomerRepo.Object);

        var paymentIntent = new PaymentIntent
        {
            Id = "pi_test_123",
            ClientSecret = "secret_123"
        };
        _mockStripeWrapper.Setup(s => s.CreatePaymentIntentAsync(It.IsAny<PaymentIntentCreateOptions>()))
            .ReturnsAsync(paymentIntent);

        var mockPaymentRepo = new Mock<IPremiumPaymentRepository>();
        _mockUnitOfWork.Setup(u => u.PremiumPayments).Returns(mockPaymentRepo.Object);
        _mockConfig.Setup(c => c["Stripe:PublishableKey"]).Returns("pk_test_123");

        // Act
        var result = await _financeService.PayPremiumAsync(customerId.ToString(), scheduleId.ToString(), request);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.PaymentIntentId, Is.EqualTo("pi_test_123"));
        Assert.That(result.ClientSecret, Is.EqualTo("secret_123"));
        Assert.That(result.PublishableKey, Is.EqualTo("pk_test_123"));

        mockPaymentRepo.Verify(r => r.AddAsync(It.IsAny<PremiumPayment>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public void PayPremiumAsync_AlreadyPaid_ThrowsException()
    {
        var scheduleId = Guid.NewGuid();
        var schedule = new PremiumSchedule { Id = scheduleId, Status = PremiumScheduleStatus.Paid };
        var mockScheduleRepo = new Mock<IRepository<PremiumSchedule>>();
        mockScheduleRepo.Setup(r => r.GetByIdAsync(scheduleId)).ReturnsAsync(schedule);
        _mockUnitOfWork.Setup(u => u.PremiumSchedules).Returns(mockScheduleRepo.Object);

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ConflictException>(() => _financeService.PayPremiumAsync(Guid.NewGuid().ToString(), scheduleId.ToString(), new CreatePaymentIntentRequest()));
    }

    [Test]
    public async Task GetPremiumScheduleAsync_ValidPolicy_ReturnsSchedules()
    {
        var policyId = Guid.NewGuid();
        var schedules = new List<PremiumSchedule>
        {
            new PremiumSchedule { Id = Guid.NewGuid(), PolicyId = policyId, Amount = 100 }
        };

        var mockScheduleRepo = new Mock<IRepository<PremiumSchedule>>();
        mockScheduleRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PremiumSchedule, bool>>>())).ReturnsAsync(schedules);
        _mockUnitOfWork.Setup(u => u.PremiumSchedules).Returns(mockScheduleRepo.Object);

        var result = await _financeService.GetPremiumScheduleAsync(policyId.ToString(), Guid.NewGuid().ToString());

        Assert.That(result.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task GetMyPaymentHistoryAsync_ValidUser_ReturnsPayments()
    {
        var customerId = Guid.NewGuid();
        var payments = new List<PremiumPayment>
        {
            new PremiumPayment { Id = Guid.NewGuid(), CustomerId = customerId, Amount = 100 }
        };

        var mockCustomerRepo = new Mock<IRepository<SpeedClaim.Api.Models.Customer>>();
        mockCustomerRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<SpeedClaim.Api.Models.Customer, bool>>>()))
            .ReturnsAsync(new SpeedClaim.Api.Models.Customer { Id = customerId, UserId = customerId });
        _mockUnitOfWork.Setup(u => u.Customers).Returns(mockCustomerRepo.Object);

        var mockPaymentRepo = new Mock<IPremiumPaymentRepository>();
        mockPaymentRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PremiumPayment, bool>>>()))
            .ReturnsAsync(payments);
        _mockUnitOfWork.Setup(u => u.PremiumPayments).Returns(mockPaymentRepo.Object);

        var result = await _financeService.GetMyPaymentHistoryAsync(customerId.ToString());

        Assert.That(result.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task GetAllPaymentRecordsAsync_ReturnsAllPayments()
    {
        var payments = new List<PremiumPayment> { new PremiumPayment { Id = Guid.NewGuid(), Amount = 100 } };
        var mockPaymentRepo = new Mock<IPremiumPaymentRepository>();
        mockPaymentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(payments);
        _mockUnitOfWork.Setup(u => u.PremiumPayments).Returns(mockPaymentRepo.Object);

        var result = await _financeService.GetAllPaymentRecordsAsync();
        Assert.That(result.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task ReconcilePaymentAsync_ValidPayment_CompletesPayment()
    {
        var paymentId = Guid.NewGuid();
        var payment = new PremiumPayment { Id = paymentId, Status = PaymentStatus.Pending, ScheduleId = Guid.NewGuid() };
        
        var schedule = new PremiumSchedule { Id = payment.ScheduleId.Value, Status = PremiumScheduleStatus.Upcoming };

        var mockPaymentRepo = new Mock<IPremiumPaymentRepository>();
        mockPaymentRepo.Setup(r => r.GetByIdAsync(paymentId)).ReturnsAsync(payment);
        _mockUnitOfWork.Setup(u => u.PremiumPayments).Returns(mockPaymentRepo.Object);

        var mockScheduleRepo = new Mock<IRepository<PremiumSchedule>>();
        mockScheduleRepo.Setup(r => r.GetByIdAsync(payment.ScheduleId.Value)).ReturnsAsync(schedule);
        _mockUnitOfWork.Setup(u => u.PremiumSchedules).Returns(mockScheduleRepo.Object);

        await _financeService.ReconcilePaymentAsync(paymentId.ToString(), Guid.NewGuid().ToString());

        Assert.That(payment.Status, Is.EqualTo(PaymentStatus.Paid));
        Assert.That(schedule.Status, Is.EqualTo(PremiumScheduleStatus.Paid));
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public async Task ProcessRefundAsync_ValidPayment_RefundsPayment()
    {
        var paymentId = Guid.NewGuid();
        var payment = new PremiumPayment { Id = paymentId, Status = PaymentStatus.Paid, ScheduleId = Guid.NewGuid() };
        var schedule = new PremiumSchedule { Id = payment.ScheduleId.Value, Status = PremiumScheduleStatus.Paid };

        var mockPaymentRepo = new Mock<IPremiumPaymentRepository>();
        mockPaymentRepo.Setup(r => r.GetByIdAsync(paymentId)).ReturnsAsync(payment);
        _mockUnitOfWork.Setup(u => u.PremiumPayments).Returns(mockPaymentRepo.Object);

        var mockScheduleRepo = new Mock<IRepository<PremiumSchedule>>();
        mockScheduleRepo.Setup(r => r.GetByIdAsync(payment.ScheduleId.Value)).ReturnsAsync(schedule);
        _mockUnitOfWork.Setup(u => u.PremiumSchedules).Returns(mockScheduleRepo.Object);

        await _financeService.ProcessRefundAsync(paymentId.ToString(), Guid.NewGuid().ToString());

        Assert.That(payment.Status, Is.EqualTo(PaymentStatus.Refunded));
        Assert.That(schedule.Status, Is.EqualTo(PremiumScheduleStatus.Overdue));
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public async Task GetPendingCommissionsAsync_ReturnsCommissions()
    {
        var commissions = new List<AgentCommission>
        {
            new AgentCommission { Id = Guid.NewGuid(), Status = "PENDING" }
        };

        var mockCommissionRepo = new Mock<IRepository<AgentCommission>>();
        mockCommissionRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<AgentCommission, bool>>>())).ReturnsAsync(commissions);
        _mockUnitOfWork.Setup(u => u.AgentCommissions).Returns(mockCommissionRepo.Object);

        var result = await _financeService.GetPendingCommissionsAsync();
        Assert.That(result.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task ApproveAndPayCommissionAsync_ValidCommission_Approves()
    {
        var commissionId = Guid.NewGuid();
        var commission = new AgentCommission { Id = commissionId, Status = "PENDING" };

        var mockCommissionRepo = new Mock<IRepository<AgentCommission>>();
        mockCommissionRepo.Setup(r => r.GetByIdAsync(commissionId)).ReturnsAsync(commission);
        _mockUnitOfWork.Setup(u => u.AgentCommissions).Returns(mockCommissionRepo.Object);

        await _financeService.ApproveAndPayCommissionAsync(commissionId.ToString(), Guid.NewGuid().ToString());

        Assert.That(commission.Status, Is.EqualTo("PAID"));
        Assert.That(commission.PaidAt, Is.Not.Null);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public async Task GetOverduePoliciesAsync_ReturnsOverdueSchedules()
    {
        var schedules = new List<PremiumSchedule>
        {
            new PremiumSchedule { Id = Guid.NewGuid(), Status = PremiumScheduleStatus.Overdue }
        };

        var mockScheduleRepo = new Mock<IRepository<PremiumSchedule>>();
        mockScheduleRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PremiumSchedule, bool>>>())).ReturnsAsync(schedules);
        _mockUnitOfWork.Setup(u => u.PremiumSchedules).Returns(mockScheduleRepo.Object);

        var result = await _financeService.GetOverduePoliciesAsync();
        Assert.That(result.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task GetPremiumCollectionSummaryAsync_ReturnsSummary()
    {
        var payments = new List<PremiumPayment>
        {
            new PremiumPayment { Amount = 100, Status = PaymentStatus.Paid },
            new PremiumPayment { Amount = 50, Status = PaymentStatus.Failed }
        };

        var mockPaymentRepo = new Mock<IPremiumPaymentRepository>();
        mockPaymentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(payments);
        _mockUnitOfWork.Setup(u => u.PremiumPayments).Returns(mockPaymentRepo.Object);

        var result = await _financeService.GetPremiumCollectionSummaryAsync("this_month");

        Assert.That(result.TotalCollected, Is.EqualTo(100));
        Assert.That(result.SuccessfulPayments, Is.EqualTo(1));
        Assert.That(result.FailedPayments, Is.EqualTo(1));
    }

    // --- DownloadReceiptAsync tests ---

    [Test]
    public void DownloadReceiptAsync_PaymentNotFound_ThrowsKeyNotFound()
    {
        var paymentId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        var mockPaymentRepo = new Mock<IPremiumPaymentRepository>();
        mockPaymentRepo.Setup(r => r.GetByIdAsync(paymentId)).ReturnsAsync((PremiumPayment?)null);
        _mockUnitOfWork.Setup(u => u.PremiumPayments).Returns(mockPaymentRepo.Object);

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.NotFoundException>(() =>
            _financeService.DownloadReceiptAsync(paymentId.ToString(), customerId.ToString()));
    }

    [Test]
    public void DownloadReceiptAsync_NotPaid_ThrowsInvalidOperation()
    {
        var paymentId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var payment = new PremiumPayment { Id = paymentId, CustomerId = customerId, Status = PaymentStatus.Pending };

        var mockCustomerRepo = new Mock<IRepository<SpeedClaim.Api.Models.Customer>>();
        mockCustomerRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<SpeedClaim.Api.Models.Customer, bool>>>()))
            .ReturnsAsync(new SpeedClaim.Api.Models.Customer { Id = customerId, UserId = customerId });
        _mockUnitOfWork.Setup(u => u.Customers).Returns(mockCustomerRepo.Object);

        var mockPaymentRepo = new Mock<IPremiumPaymentRepository>();
        mockPaymentRepo.Setup(r => r.GetByIdAsync(paymentId)).ReturnsAsync(payment);
        _mockUnitOfWork.Setup(u => u.PremiumPayments).Returns(mockPaymentRepo.Object);

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.UnprocessableException>(() =>
            _financeService.DownloadReceiptAsync(paymentId.ToString(), customerId.ToString()));
    }

    [Test]
    public async Task DownloadReceiptAsync_ValidPaidPayment_ReturnsReceipt()
    {
        var paymentId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var payment = new PremiumPayment
        {
            Id = paymentId,
            CustomerId = customerId,
            Status = PaymentStatus.Paid,
            Amount = 250,
            Currency = "USD",
            ReceiptUrl = "https://stripe.com/receipt/123"
        };

        var mockCustomerRepo = new Mock<IRepository<SpeedClaim.Api.Models.Customer>>();
        mockCustomerRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<SpeedClaim.Api.Models.Customer, bool>>>()))
            .ReturnsAsync(new SpeedClaim.Api.Models.Customer { Id = customerId, UserId = customerId });
        _mockUnitOfWork.Setup(u => u.Customers).Returns(mockCustomerRepo.Object);

        var mockPaymentRepo = new Mock<IPremiumPaymentRepository>();
        mockPaymentRepo.Setup(r => r.GetByIdAsync(paymentId)).ReturnsAsync(payment);
        _mockUnitOfWork.Setup(u => u.PremiumPayments).Returns(mockPaymentRepo.Object);

        var result = await _financeService.DownloadReceiptAsync(paymentId.ToString(), customerId.ToString());

        Assert.That(result.Id, Is.EqualTo(paymentId));
        Assert.That(result.Amount, Is.EqualTo(250));
        Assert.That(result.ReceiptUrl, Is.EqualTo("https://stripe.com/receipt/123"));
        Assert.That(result.Status, Is.EqualTo("Paid"));
    }

    // --- ProcessClaimPayoutAsync tests ---

    [Test]
    public void ProcessClaimPayoutAsync_ClaimNotFound_ThrowsKeyNotFound()
    {
        var claimId = Guid.NewGuid();
        var mockClaimRepo = new Mock<IClaimRepository>();
        mockClaimRepo.Setup(r => r.GetByIdAsync(claimId)).ReturnsAsync((Claim?)null);
        _mockUnitOfWork.Setup(u => u.Claims).Returns(mockClaimRepo.Object);

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.NotFoundException>(() =>
            _financeService.ProcessClaimPayoutAsync(claimId.ToString(), Guid.NewGuid().ToString()));
    }

    [Test]
    public void ProcessClaimPayoutAsync_ClaimNotApproved_ThrowsInvalidOperation()
    {
        var claimId = Guid.NewGuid();
        var claim = new Claim { Id = claimId, Status = ClaimStatus.Intimated };

        var mockClaimRepo = new Mock<IClaimRepository>();
        mockClaimRepo.Setup(r => r.GetByIdAsync(claimId)).ReturnsAsync(claim);
        _mockUnitOfWork.Setup(u => u.Claims).Returns(mockClaimRepo.Object);

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.UnprocessableException>(() =>
            _financeService.ProcessClaimPayoutAsync(claimId.ToString(), Guid.NewGuid().ToString()));
    }

    [Test]
    public async Task ProcessClaimPayoutAsync_ValidApprovedClaim_MarksSettledAndCreatesIntent()
    {
        var claimId = Guid.NewGuid();
        var officerId = Guid.NewGuid();
        var claim = new Claim
        {
            Id = claimId,
            ClaimNumber = "CLM-001",
            Status = ClaimStatus.Approved,
            ClaimAmountApproved = 500
        };

        var mockClaimRepo = new Mock<IClaimRepository>();
        mockClaimRepo.Setup(r => r.GetByIdAsync(claimId)).ReturnsAsync(claim);
        _mockUnitOfWork.Setup(u => u.Claims).Returns(mockClaimRepo.Object);

        var mockHistoryRepo = new Mock<IRepository<ClaimStatusHistory>>();
        _mockUnitOfWork.Setup(u => u.ClaimStatusHistories).Returns(mockHistoryRepo.Object);

        var paymentIntent = new PaymentIntent { Id = "pi_payout_123" };
        _mockStripeWrapper.Setup(s => s.CreatePaymentIntentAsync(It.IsAny<PaymentIntentCreateOptions>()))
            .ReturnsAsync(paymentIntent);
        _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

        await _financeService.ProcessClaimPayoutAsync(claimId.ToString(), officerId.ToString());

        Assert.That(claim.Status, Is.EqualTo(ClaimStatus.Settled));
        Assert.That(claim.SettlementDate, Is.Not.Null);
        _mockStripeWrapper.Verify(s => s.CreatePaymentIntentAsync(It.Is<PaymentIntentCreateOptions>(
            o => o.Amount == 50000 && o.Currency == "usd")), Times.Once);
        mockHistoryRepo.Verify(r => r.AddAsync(It.IsAny<ClaimStatusHistory>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    // --- MarkClaimFinanciallySettledAsync tests ---

    [Test]
    public void MarkClaimFinanciallySettledAsync_AlreadySettled_ThrowsInvalidOperation()
    {
        var claimId = Guid.NewGuid();
        var claim = new Claim { Id = claimId, Status = ClaimStatus.Settled };

        var mockClaimRepo = new Mock<IClaimRepository>();
        mockClaimRepo.Setup(r => r.GetByIdAsync(claimId)).ReturnsAsync(claim);
        _mockUnitOfWork.Setup(u => u.Claims).Returns(mockClaimRepo.Object);

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ConflictException>(() =>
            _financeService.MarkClaimFinanciallySettledAsync(claimId.ToString(), Guid.NewGuid().ToString()));
    }

    [Test]
    public async Task MarkClaimFinanciallySettledAsync_ValidClaim_SetsSettledStatus()
    {
        var claimId = Guid.NewGuid();
        var officerId = Guid.NewGuid();
        var claim = new Claim { Id = claimId, Status = ClaimStatus.Approved };

        var mockClaimRepo = new Mock<IClaimRepository>();
        mockClaimRepo.Setup(r => r.GetByIdAsync(claimId)).ReturnsAsync(claim);
        _mockUnitOfWork.Setup(u => u.Claims).Returns(mockClaimRepo.Object);

        var mockHistoryRepo = new Mock<IRepository<ClaimStatusHistory>>();
        _mockUnitOfWork.Setup(u => u.ClaimStatusHistories).Returns(mockHistoryRepo.Object);
        _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

        await _financeService.MarkClaimFinanciallySettledAsync(claimId.ToString(), officerId.ToString());

        Assert.That(claim.Status, Is.EqualTo(ClaimStatus.Settled));
        Assert.That(claim.SettlementDate, Is.Not.Null);
        mockHistoryRepo.Verify(r => r.AddAsync(It.IsAny<ClaimStatusHistory>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    // --- ExportPaymentReportsAsync test ---

    [Test]
    public async Task ExportPaymentReportsAsync_ReturnsNonEmptyXlsxBytes()
    {
        var payments = new List<PremiumPayment>
        {
            new PremiumPayment
            {
                Id = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                Amount = 100,
                Currency = "USD",
                PaymentType = PaymentType.FirstPremium,
                Status = PaymentStatus.Paid,
                PaidAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        var mockPaymentRepo = new Mock<IPremiumPaymentRepository>();
        mockPaymentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(payments);
        _mockUnitOfWork.Setup(u => u.PremiumPayments).Returns(mockPaymentRepo.Object);

        var result = await _financeService.ExportPaymentReportsAsync();

        // Should be valid XLSX bytes — XLSX signature starts with PK (0x50 0x4B)
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Length, Is.GreaterThan(0));
        Assert.That(result[0], Is.EqualTo(0x50)); // 'P'
        Assert.That(result[1], Is.EqualTo(0x4B)); // 'K'
    }

    [Test]
    public async Task GetSavedPaymentMethodsAsync_WithStripeCustomer_ReturnsMappedCards()
    {
        var customerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var customerRecord = new SpeedClaim.Api.Models.Customer { Id = customerId, UserId = userId };
        var stripeRecord = new SpeedClaim.Api.Models.StripeCustomer { Id = Guid.NewGuid(), UserId = userId, StripeCustomerId = "cus_123" };

        var mockCustomerRepo = new Mock<IRepository<SpeedClaim.Api.Models.Customer>>();
        var mockStripeRepo = new Mock<IRepository<SpeedClaim.Api.Models.StripeCustomer>>();
        mockCustomerRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<SpeedClaim.Api.Models.Customer, bool>>>())).ReturnsAsync(customerRecord);
        mockStripeRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<SpeedClaim.Api.Models.StripeCustomer, bool>>>())).ReturnsAsync(stripeRecord);
        _mockUnitOfWork.Setup(u => u.Customers).Returns(mockCustomerRepo.Object);
        _mockUnitOfWork.Setup(u => u.StripeCustomers).Returns(mockStripeRepo.Object);

        var card = new Stripe.PaymentMethod
        {
            Id = "pm_001",
            Card = new Stripe.PaymentMethodCard { Brand = "visa", Last4 = "4242", ExpMonth = 12, ExpYear = 2026 }
        };
        var stripeList = new Stripe.StripeList<Stripe.PaymentMethod> { Data = new System.Collections.Generic.List<Stripe.PaymentMethod> { card } };
        _mockStripeWrapper.Setup(s => s.ListPaymentMethodsAsync("cus_123", "card")).ReturnsAsync(stripeList);

        var result = (await _financeService.GetSavedPaymentMethodsAsync(customerId.ToString())).ToList();

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].Brand, Is.EqualTo("visa"));
        Assert.That(result[0].Last4, Is.EqualTo("4242"));
    }

    [Test]
    public async Task GetSavedPaymentMethodsAsync_NoStripeCustomer_ReturnsEmpty()
    {
        var customerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var customerRecord = new SpeedClaim.Api.Models.Customer { Id = customerId, UserId = userId };

        var mockCustomerRepo = new Mock<IRepository<SpeedClaim.Api.Models.Customer>>();
        var mockStripeRepo = new Mock<IRepository<SpeedClaim.Api.Models.StripeCustomer>>();
        mockCustomerRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<SpeedClaim.Api.Models.Customer, bool>>>())).ReturnsAsync(customerRecord);
        mockStripeRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<SpeedClaim.Api.Models.StripeCustomer, bool>>>())).ReturnsAsync((SpeedClaim.Api.Models.StripeCustomer?)null);
        _mockUnitOfWork.Setup(u => u.Customers).Returns(mockCustomerRepo.Object);
        _mockUnitOfWork.Setup(u => u.StripeCustomers).Returns(mockStripeRepo.Object);

        var result = await _financeService.GetSavedPaymentMethodsAsync(customerId.ToString());

        Assert.That(result.Count(), Is.EqualTo(0));
    }

    [Test]
    public void GetSavedPaymentMethodsAsync_CustomerNotFound_ThrowsKeyNotFound()
    {
        var customerId = Guid.NewGuid();
        var mockCustomerRepo = new Mock<IRepository<SpeedClaim.Api.Models.Customer>>();
        mockCustomerRepo.Setup(r => r.GetByIdAsync(customerId)).ReturnsAsync((SpeedClaim.Api.Models.Customer?)null);
        _mockUnitOfWork.Setup(u => u.Customers).Returns(mockCustomerRepo.Object);

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.NotFoundException>(() => _financeService.GetSavedPaymentMethodsAsync(customerId.ToString()));
    }

    [Test]
    public async Task PayPremiumAsync_NoExistingStripeCustomer_CreatesCustomerAndReturnsIntent()
    {
        var customerId = Guid.NewGuid();
        var scheduleId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var request = new CreatePaymentIntentRequest { PolicyId = Guid.NewGuid() };

        var schedule = new PremiumSchedule { Id = scheduleId, Amount = 200m, Status = PremiumScheduleStatus.Upcoming, InstallmentNumber = 2 };
        var mockScheduleRepo = new Mock<IRepository<PremiumSchedule>>();
        mockScheduleRepo.Setup(r => r.GetByIdAsync(scheduleId)).ReturnsAsync(schedule);
        _mockUnitOfWork.Setup(u => u.PremiumSchedules).Returns(mockScheduleRepo.Object);

        var customerEntity = new SpeedClaim.Api.Models.Customer { Id = customerId, UserId = userId };
        var mockCustomerRepo = new Mock<IRepository<SpeedClaim.Api.Models.Customer>>();
        mockCustomerRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<SpeedClaim.Api.Models.Customer, bool>>>())).ReturnsAsync(customerEntity);
        _mockUnitOfWork.Setup(u => u.Customers).Returns(mockCustomerRepo.Object);

        var mockStripeCustomerRepo = new Mock<IRepository<SpeedClaim.Api.Models.StripeCustomer>>();
        mockStripeCustomerRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<SpeedClaim.Api.Models.StripeCustomer, bool>>>()))
            .ReturnsAsync((SpeedClaim.Api.Models.StripeCustomer?)null);
        _mockUnitOfWork.Setup(u => u.StripeCustomers).Returns(mockStripeCustomerRepo.Object);

        var user = new User { Id = userId, Email = "u@test.com", FirstName = "U", LastName = "S" };
        var mockUserRepo = new Mock<IUserRepository>();
        mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockUnitOfWork.Setup(u => u.Users).Returns(mockUserRepo.Object);

        _mockStripeWrapper.Setup(s => s.CreateCustomerAsync(It.IsAny<Stripe.CustomerCreateOptions>()))
            .ReturnsAsync(new Stripe.Customer { Id = "cus_new_123" });

        var paymentIntent = new Stripe.PaymentIntent { Id = "pi_new_123", ClientSecret = "secret_new" };
        _mockStripeWrapper.Setup(s => s.CreatePaymentIntentAsync(It.IsAny<Stripe.PaymentIntentCreateOptions>()))
            .ReturnsAsync(paymentIntent);

        var mockPaymentRepo = new Mock<IPremiumPaymentRepository>();
        _mockUnitOfWork.Setup(u => u.PremiumPayments).Returns(mockPaymentRepo.Object);
        _mockConfig.Setup(c => c["Stripe:PublishableKey"]).Returns("pk_test");

        var result = await _financeService.PayPremiumAsync(customerId.ToString(), scheduleId.ToString(), request);

        Assert.That(result.PaymentIntentId, Is.EqualTo("pi_new_123"));
        mockStripeCustomerRepo.Verify(r => r.AddAsync(It.IsAny<SpeedClaim.Api.Models.StripeCustomer>()), Times.Once);
    }

    [Test]
    public async Task ReconcileByStripeIntentAsync_PaymentNotFound_DoesNothing()
    {
        var mockPaymentRepo = new Mock<IPremiumPaymentRepository>();
        mockPaymentRepo.Setup(r => r.GetByIntentWithScheduleAsync("pi_unknown")).ReturnsAsync((PremiumPayment?)null);
        _mockUnitOfWork.Setup(u => u.PremiumPayments).Returns(mockPaymentRepo.Object);

        await _financeService.ReconcileByStripeIntentAsync("pi_unknown");

        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Never);
    }

    [Test]
    public async Task ReconcilePaymentAsync_PendingPayment_MarksAsPaid()
    {
        var paymentId = Guid.NewGuid();
        var payment = new PremiumPayment { Id = paymentId, Status = PaymentStatus.Pending, ScheduleId = null };
        var mockPaymentRepo = new Mock<IPremiumPaymentRepository>();
        mockPaymentRepo.Setup(r => r.GetByIdAsync(paymentId)).ReturnsAsync(payment);
        _mockUnitOfWork.Setup(u => u.PremiumPayments).Returns(mockPaymentRepo.Object);

        await _financeService.ReconcilePaymentAsync(paymentId.ToString(), "officer123");

        Assert.That(payment.Status, Is.EqualTo(PaymentStatus.Paid));
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public void PayPremiumAsync_InvalidIds_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ValidationException>(() =>
            _financeService.PayPremiumAsync("not-a-guid", Guid.NewGuid().ToString(), new CreatePaymentIntentRequest()));
    }

    [Test]
    public async Task ReconcilePaymentAsync_WithScheduleAndPendingPolicy_ActivatesPolicy()
    {
        var paymentId = Guid.NewGuid();
        var scheduleId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var payment = new PremiumPayment { Id = paymentId, Status = PaymentStatus.Pending, ScheduleId = scheduleId };
        var schedule = new PremiumSchedule { Id = scheduleId, Status = PremiumScheduleStatus.Upcoming, PolicyId = policyId };
        var policy = new Policy { Id = policyId, CustomerId = customerId, Status = PolicyStatus.Pending, PolicyNumber = "POL-001" };
        var customer = new SpeedClaim.Api.Models.Customer { Id = customerId, UserId = userId };
        var user = new User { Id = userId, Email = "test@test.com", FirstName = "Test" };

        var mockPaymentRepo = new Mock<IPremiumPaymentRepository>();
        mockPaymentRepo.Setup(r => r.GetByIdAsync(paymentId)).ReturnsAsync(payment);
        _mockUnitOfWork.Setup(u => u.PremiumPayments).Returns(mockPaymentRepo.Object);

        var mockScheduleRepo = new Mock<IRepository<PremiumSchedule>>();
        mockScheduleRepo.Setup(r => r.GetByIdAsync(scheduleId)).ReturnsAsync(schedule);
        _mockUnitOfWork.Setup(u => u.PremiumSchedules).Returns(mockScheduleRepo.Object);

        var mockPolicyRepo = new Mock<IPolicyRepository>();
        mockPolicyRepo.Setup(r => r.GetByIdAsync(policyId)).ReturnsAsync(policy);
        _mockUnitOfWork.Setup(u => u.Policies).Returns(mockPolicyRepo.Object);

        var mockCustomerRepo = new Mock<IRepository<SpeedClaim.Api.Models.Customer>>();
        mockCustomerRepo.Setup(r => r.GetByIdAsync(customerId)).ReturnsAsync(customer);
        _mockUnitOfWork.Setup(u => u.Customers).Returns(mockCustomerRepo.Object);

        var mockUserRepo = new Mock<IUserRepository>();
        mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockUnitOfWork.Setup(u => u.Users).Returns(mockUserRepo.Object);

        _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

        await _financeService.ReconcilePaymentAsync(paymentId.ToString(), Guid.NewGuid().ToString());

        Assert.That(policy.Status, Is.EqualTo(PolicyStatus.Active));
        Assert.That(payment.Status, Is.EqualTo(PaymentStatus.Paid));
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public void ProcessRefundAsync_NotFound_ThrowsNotFoundException()
    {
        var paymentId = Guid.NewGuid();
        var mockPaymentRepo = new Mock<IPremiumPaymentRepository>();
        mockPaymentRepo.Setup(r => r.GetByIdAsync(paymentId)).ReturnsAsync((PremiumPayment?)null);
        _mockUnitOfWork.Setup(u => u.PremiumPayments).Returns(mockPaymentRepo.Object);

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.NotFoundException>(() =>
            _financeService.ProcessRefundAsync(paymentId.ToString(), Guid.NewGuid().ToString()));
    }

    [Test]
    public void ApproveAndPayCommissionAsync_NotFound_ThrowsNotFoundException()
    {
        var commissionId = Guid.NewGuid();
        var mockCommissionRepo = new Mock<IRepository<AgentCommission>>();
        mockCommissionRepo.Setup(r => r.GetByIdAsync(commissionId)).ReturnsAsync((AgentCommission?)null);
        _mockUnitOfWork.Setup(u => u.AgentCommissions).Returns(mockCommissionRepo.Object);

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.NotFoundException>(() =>
            _financeService.ApproveAndPayCommissionAsync(commissionId.ToString(), Guid.NewGuid().ToString()));
    }

    [Test]
    public async Task ReconcileByStripeIntentAsync_WithFoundPayment_MarksAsPaid()
    {
        var paymentId = Guid.NewGuid();
        var payment = new PremiumPayment { Id = paymentId, Status = PaymentStatus.Pending, ScheduleId = null };

        var mockPaymentRepo = new Mock<IPremiumPaymentRepository>();
        mockPaymentRepo.Setup(r => r.GetByIntentWithScheduleAsync("pi_test")).ReturnsAsync(payment);
        mockPaymentRepo.Setup(r => r.GetByIdAsync(paymentId)).ReturnsAsync(payment);
        _mockUnitOfWork.Setup(u => u.PremiumPayments).Returns(mockPaymentRepo.Object);
        _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

        await _financeService.ReconcileByStripeIntentAsync("pi_test");

        Assert.That(payment.Status, Is.EqualTo(PaymentStatus.Paid));
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public async Task ReconcileByStripeIntentAsync_WithChargeId_SetsReceiptUrl()
    {
        var paymentId = Guid.NewGuid();
        var payment = new PremiumPayment { Id = paymentId, Status = PaymentStatus.Pending, ScheduleId = null };

        var mockPaymentRepo = new Mock<IPremiumPaymentRepository>();
        mockPaymentRepo.Setup(r => r.GetByIntentWithScheduleAsync("pi_test")).ReturnsAsync(payment);
        mockPaymentRepo.Setup(r => r.GetByIdAsync(paymentId)).ReturnsAsync(payment);
        _mockUnitOfWork.Setup(u => u.PremiumPayments).Returns(mockPaymentRepo.Object);
        _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);
        _mockStripeWrapper.Setup(s => s.GetChargeAsync("ch_test"))
            .ReturnsAsync(new Charge { Id = "ch_test", ReceiptUrl = "https://receipts.stripe.com/live_001" });

        await _financeService.ReconcileByStripeIntentAsync("pi_test", "ch_test");

        Assert.That(payment.Status, Is.EqualTo(PaymentStatus.Paid));
        Assert.That(payment.ReceiptUrl, Is.EqualTo("https://receipts.stripe.com/live_001"));
        Assert.That(payment.StripeChargeId, Is.EqualTo("ch_test"));
    }

    [Test]
    public void ProcessClaimPayoutAsync_ZeroApprovedAmount_ThrowsValidationException()
    {
        var claimId = Guid.NewGuid();
        var claim = new Claim
        {
            Id = claimId,
            Status = ClaimStatus.Approved,
            ClaimAmountApproved = 0
        };
        var mockClaimRepo = new Mock<IClaimRepository>();
        mockClaimRepo.Setup(r => r.GetByIdAsync(claimId)).ReturnsAsync(claim);
        _mockUnitOfWork.Setup(u => u.Claims).Returns(mockClaimRepo.Object);

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ValidationException>(() =>
            _financeService.ProcessClaimPayoutAsync(claimId.ToString(), Guid.NewGuid().ToString()));
    }
}
