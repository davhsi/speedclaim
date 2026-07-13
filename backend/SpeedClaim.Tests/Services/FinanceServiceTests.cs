using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Npgsql;
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
        _mockUnitOfWork.Setup(u => u.Policies).Returns(new Mock<IPolicyRepository>().Object);
        _mockUnitOfWork.Setup(u => u.PolicyStatusHistories).Returns(new Mock<IRepository<PolicyStatusHistory>>().Object);
        _mockUnitOfWork.Setup(u => u.Agents).Returns(new Mock<IRepository<Agent>>().Object);
        _mockUnitOfWork.Setup(u => u.InsuranceProducts).Returns(new Mock<IRepository<InsuranceProduct>>().Object);
        _mockUnitOfWork.Setup(u => u.AuditLogs).Returns(new Mock<IRepository<AuditLog>>().Object);

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

        var mockPolicyRepo = new Mock<IPolicyRepository>();
        mockPolicyRepo.Setup(r => r.GetByIdAsync(policyId)).ReturnsAsync(new Policy { Id = policyId, CustomerId = customerEntity.Id });
        _mockUnitOfWork.Setup(u => u.Policies).Returns(mockPolicyRepo.Object);

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
        _mockStripeWrapper.Setup(s => s.CreatePaymentIntentAsync(It.IsAny<PaymentIntentCreateOptions>(), It.IsAny<RequestOptions>()))
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
    public void PayPremiumAsync_OtherCustomersSchedule_ThrowsForbiddenException()
    {
        var customerUserId = Guid.NewGuid();
        var customerRecordId = Guid.NewGuid();
        var scheduleId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
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

        var mockCustomerRepo = new Mock<IRepository<SpeedClaim.Api.Models.Customer>>();
        mockCustomerRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<SpeedClaim.Api.Models.Customer, bool>>>()))
            .ReturnsAsync(new SpeedClaim.Api.Models.Customer { Id = customerRecordId, UserId = customerUserId });
        _mockUnitOfWork.Setup(u => u.Customers).Returns(mockCustomerRepo.Object);

        var mockPolicyRepo = new Mock<IPolicyRepository>();
        mockPolicyRepo.Setup(r => r.GetByIdAsync(policyId)).ReturnsAsync(new Policy { Id = policyId, CustomerId = Guid.NewGuid() });
        _mockUnitOfWork.Setup(u => u.Policies).Returns(mockPolicyRepo.Object);

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ForbiddenException>(() =>
            _financeService.PayPremiumAsync(customerUserId.ToString(), scheduleId.ToString(), new CreatePaymentIntentRequest { PolicyId = policyId }));
        _mockStripeWrapper.Verify(s => s.CreatePaymentIntentAsync(It.IsAny<PaymentIntentCreateOptions>(), It.IsAny<RequestOptions>()), Times.Never);
    }

    [Test]
    public void PayPremiumAsync_LaterInstallmentWithEarlierUnpaidSchedule_ThrowsConflictException()
    {
        var customerUserId = Guid.NewGuid();
        var customerRecordId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        var firstSchedule = new PremiumSchedule
        {
            Id = Guid.NewGuid(),
            PolicyId = policyId,
            Amount = 150m,
            Status = PremiumScheduleStatus.Upcoming,
            InstallmentNumber = 1
        };
        var secondSchedule = new PremiumSchedule
        {
            Id = Guid.NewGuid(),
            PolicyId = policyId,
            Amount = 150m,
            Status = PremiumScheduleStatus.Upcoming,
            InstallmentNumber = 2
        };

        var mockScheduleRepo = new Mock<IRepository<PremiumSchedule>>();
        mockScheduleRepo.Setup(r => r.GetByIdAsync(secondSchedule.Id)).ReturnsAsync(secondSchedule);
        mockScheduleRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<PremiumSchedule, bool>>>()))
            .ReturnsAsync(new List<PremiumSchedule> { firstSchedule, secondSchedule });
        _mockUnitOfWork.Setup(u => u.PremiumSchedules).Returns(mockScheduleRepo.Object);

        var mockCustomerRepo = new Mock<IRepository<SpeedClaim.Api.Models.Customer>>();
        mockCustomerRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<SpeedClaim.Api.Models.Customer, bool>>>()))
            .ReturnsAsync(new SpeedClaim.Api.Models.Customer { Id = customerRecordId, UserId = customerUserId });
        _mockUnitOfWork.Setup(u => u.Customers).Returns(mockCustomerRepo.Object);

        var mockPolicyRepo = new Mock<IPolicyRepository>();
        mockPolicyRepo.Setup(r => r.GetByIdAsync(policyId)).ReturnsAsync(new Policy { Id = policyId, CustomerId = customerRecordId });
        _mockUnitOfWork.Setup(u => u.Policies).Returns(mockPolicyRepo.Object);

        var ex = Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ConflictException>(() =>
            _financeService.PayPremiumAsync(customerUserId.ToString(), secondSchedule.Id.ToString(), new CreatePaymentIntentRequest { PolicyId = policyId }));

        Assert.That(ex!.Message, Is.EqualTo("Installment #1 must be paid before installment #2."));
        _mockStripeWrapper.Verify(s => s.CreatePaymentIntentAsync(It.IsAny<PaymentIntentCreateOptions>(), It.IsAny<RequestOptions>()), Times.Never);
    }

    [Test]
    public async Task GetPremiumScheduleAsync_ValidPolicy_ReturnsSchedules()
    {
        var customerUserId = Guid.NewGuid();
        var customerRecordId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        var schedules = new List<PremiumSchedule>
        {
            new PremiumSchedule { Id = Guid.NewGuid(), PolicyId = policyId, InstallmentNumber = 2, Amount = 200 },
            new PremiumSchedule { Id = Guid.NewGuid(), PolicyId = policyId, InstallmentNumber = 1, Amount = 100 }
        };

        var mockCustomerRepo = new Mock<IRepository<SpeedClaim.Api.Models.Customer>>();
        mockCustomerRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<SpeedClaim.Api.Models.Customer, bool>>>()))
            .ReturnsAsync(new SpeedClaim.Api.Models.Customer { Id = customerRecordId, UserId = customerUserId });
        _mockUnitOfWork.Setup(u => u.Customers).Returns(mockCustomerRepo.Object);

        var mockPolicyRepo = new Mock<IPolicyRepository>();
        mockPolicyRepo.Setup(r => r.GetByIdAsync(policyId)).ReturnsAsync(new Policy { Id = policyId, CustomerId = customerRecordId });
        _mockUnitOfWork.Setup(u => u.Policies).Returns(mockPolicyRepo.Object);

        var mockScheduleRepo = new Mock<IRepository<PremiumSchedule>>();
        mockScheduleRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PremiumSchedule, bool>>>())).ReturnsAsync(schedules);
        _mockUnitOfWork.Setup(u => u.PremiumSchedules).Returns(mockScheduleRepo.Object);

        var result = await _financeService.GetPremiumScheduleAsync(policyId.ToString(), customerUserId.ToString());

        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result.Select(s => s.InstallmentNumber), Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public async Task GetPremiumScheduleAsync_IncompleteMonthlySchedule_BackfillsUpcomingInstallments()
    {
        var customerUserId = Guid.NewGuid();
        var customerRecordId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        var existingSchedule = new PremiumSchedule
        {
            Id = Guid.NewGuid(),
            PolicyId = policyId,
            InstallmentNumber = 1,
            DueDate = new DateTime(2026, 7, 14),
            Amount = 8500,
            Status = PremiumScheduleStatus.Paid
        };

        var mockCustomerRepo = new Mock<IRepository<SpeedClaim.Api.Models.Customer>>();
        mockCustomerRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<SpeedClaim.Api.Models.Customer, bool>>>()))
            .ReturnsAsync(new SpeedClaim.Api.Models.Customer { Id = customerRecordId, UserId = customerUserId });
        _mockUnitOfWork.Setup(u => u.Customers).Returns(mockCustomerRepo.Object);

        var mockPolicyRepo = new Mock<IPolicyRepository>();
        mockPolicyRepo.Setup(r => r.GetByIdAsync(policyId)).ReturnsAsync(new Policy
        {
            Id = policyId,
            CustomerId = customerRecordId,
            PaymentFrequency = "Monthly",
            PremiumAmount = 8500,
            StartDate = new DateTime(2026, 7, 14),
            EndDate = new DateTime(2027, 7, 14)
        });
        _mockUnitOfWork.Setup(u => u.Policies).Returns(mockPolicyRepo.Object);

        List<PremiumSchedule> created = [];
        var mockScheduleRepo = new Mock<IRepository<PremiumSchedule>>();
        mockScheduleRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<PremiumSchedule, bool>>>()))
            .ReturnsAsync(new List<PremiumSchedule> { existingSchedule });
        mockScheduleRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PremiumSchedule>>()))
            .Callback<IEnumerable<PremiumSchedule>>(items => created = items.ToList())
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.PremiumSchedules).Returns(mockScheduleRepo.Object);

        var result = (await _financeService.GetPremiumScheduleAsync(policyId.ToString(), customerUserId.ToString())).ToList();

        Assert.That(result, Has.Count.EqualTo(12));
        Assert.That(result.First().Status, Is.EqualTo("Paid"));
        Assert.That(created, Has.Count.EqualTo(11));
        Assert.That(created.First().InstallmentNumber, Is.EqualTo(2));
        Assert.That(created.First().DueDate, Is.EqualTo(new DateTime(2026, 8, 14)));
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public void GetPremiumScheduleAsync_OtherCustomersPolicy_ThrowsForbiddenException()
    {
        var customerUserId = Guid.NewGuid();
        var policyId = Guid.NewGuid();

        var mockCustomerRepo = new Mock<IRepository<SpeedClaim.Api.Models.Customer>>();
        mockCustomerRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<SpeedClaim.Api.Models.Customer, bool>>>()))
            .ReturnsAsync(new SpeedClaim.Api.Models.Customer { Id = Guid.NewGuid(), UserId = customerUserId });
        _mockUnitOfWork.Setup(u => u.Customers).Returns(mockCustomerRepo.Object);

        var mockPolicyRepo = new Mock<IPolicyRepository>();
        mockPolicyRepo.Setup(r => r.GetByIdAsync(policyId)).ReturnsAsync(new Policy { Id = policyId, CustomerId = Guid.NewGuid() });
        _mockUnitOfWork.Setup(u => u.Policies).Returns(mockPolicyRepo.Object);

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ForbiddenException>(() =>
            _financeService.GetPremiumScheduleAsync(policyId.ToString(), customerUserId.ToString()));
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
        var agentId = Guid.NewGuid();
        var agentUserId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var commissions = new List<AgentCommission>
        {
            new AgentCommission
            {
                Id = Guid.NewGuid(),
                AgentId = agentId,
                PolicyId = policyId,
                CommissionRate = 0.05m,
                CommissionAmount = 125,
                Status = "PENDING"
            }
        };

        var mockCommissionRepo = new Mock<IRepository<AgentCommission>>();
        mockCommissionRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<AgentCommission, bool>>>())).ReturnsAsync(commissions);
        _mockUnitOfWork.Setup(u => u.AgentCommissions).Returns(mockCommissionRepo.Object);
        var mockAgentRepo = new Mock<IRepository<Agent>>();
        mockAgentRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Agent, bool>>>()))
            .ReturnsAsync(new List<Agent> { new Agent { Id = agentId, UserId = agentUserId } });
        _mockUnitOfWork.Setup(u => u.Agents).Returns(mockAgentRepo.Object);
        var mockPolicyRepo = new Mock<IPolicyRepository>();
        mockPolicyRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Policy, bool>>>()))
            .ReturnsAsync(new List<Policy> { new Policy { Id = policyId, PolicyNumber = "POL-001", ProductId = productId } });
        _mockUnitOfWork.Setup(u => u.Policies).Returns(mockPolicyRepo.Object);
        var mockUserRepo = new Mock<IUserRepository>();
        mockUserRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(new List<User> { new User { Id = agentUserId, FirstName = "Agent", LastName = "One" } });
        _mockUnitOfWork.Setup(u => u.Users).Returns(mockUserRepo.Object);
        var mockProductRepo = new Mock<IRepository<InsuranceProduct>>();
        mockProductRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<InsuranceProduct, bool>>>()))
            .ReturnsAsync(new List<InsuranceProduct> { new InsuranceProduct { Id = productId, Domain = "Health" } });
        _mockUnitOfWork.Setup(u => u.InsuranceProducts).Returns(mockProductRepo.Object);

        var result = (await _financeService.GetPendingCommissionsAsync()).ToList();
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].Status, Is.EqualTo("Pending"));
        Assert.That(result[0].AgentName, Is.EqualTo("Agent One"));
        Assert.That(result[0].PolicyNumber, Is.EqualTo("POL-001"));
        Assert.That(result[0].Domain, Is.EqualTo("Health"));
        Assert.That(result[0].CommissionRate, Is.EqualTo(5));
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
    public void ApproveAndPayCommissionAsync_AlreadyPaid_ThrowsConflictException()
    {
        var commissionId = Guid.NewGuid();
        var commission = new AgentCommission { Id = commissionId, Status = "PAID" };

        var mockCommissionRepo = new Mock<IRepository<AgentCommission>>();
        mockCommissionRepo.Setup(r => r.GetByIdAsync(commissionId)).ReturnsAsync(commission);
        _mockUnitOfWork.Setup(u => u.AgentCommissions).Returns(mockCommissionRepo.Object);

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ConflictException>(() =>
            _financeService.ApproveAndPayCommissionAsync(commissionId.ToString(), Guid.NewGuid().ToString()));
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Never);
    }

    [Test]
    public async Task GetOverduePoliciesAsync_ReturnsOverdueSchedules()
    {
        var policyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var schedules = new List<PremiumSchedule>
        {
            new PremiumSchedule
            {
                Id = Guid.NewGuid(),
                PolicyId = policyId,
                Amount = 300,
                DueDate = DateTime.UtcNow.Date.AddDays(-12),
                Status = PremiumScheduleStatus.Overdue
            }
        };

        var mockScheduleRepo = new Mock<IRepository<PremiumSchedule>>();
        mockScheduleRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PremiumSchedule, bool>>>())).ReturnsAsync(schedules);
        _mockUnitOfWork.Setup(u => u.PremiumSchedules).Returns(mockScheduleRepo.Object);
        var mockPolicyRepo = new Mock<IPolicyRepository>();
        mockPolicyRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Policy, bool>>>()))
            .ReturnsAsync(new List<Policy> { new Policy { Id = policyId, CustomerId = customerId, ProductId = productId, PolicyNumber = "POL-OD" } });
        _mockUnitOfWork.Setup(u => u.Policies).Returns(mockPolicyRepo.Object);
        var mockCustomerRepo = new Mock<IRepository<SpeedClaim.Api.Models.Customer>>();
        mockCustomerRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<SpeedClaim.Api.Models.Customer, bool>>>()))
            .ReturnsAsync(new List<SpeedClaim.Api.Models.Customer> { new SpeedClaim.Api.Models.Customer { Id = customerId, UserId = userId } });
        _mockUnitOfWork.Setup(u => u.Customers).Returns(mockCustomerRepo.Object);
        var mockUserRepo = new Mock<IUserRepository>();
        mockUserRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(new List<User> { new User { Id = userId, FirstName = "Customer", LastName = "One" } });
        _mockUnitOfWork.Setup(u => u.Users).Returns(mockUserRepo.Object);
        var mockProductRepo = new Mock<IRepository<InsuranceProduct>>();
        mockProductRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<InsuranceProduct, bool>>>()))
            .ReturnsAsync(new List<InsuranceProduct> { new InsuranceProduct { Id = productId, Domain = "Motor" } });
        _mockUnitOfWork.Setup(u => u.InsuranceProducts).Returns(mockProductRepo.Object);

        var result = (await _financeService.GetOverduePoliciesAsync()).ToList();
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].PolicyNumber, Is.EqualTo("POL-OD"));
        Assert.That(result[0].CustomerName, Is.EqualTo("Customer One"));
        Assert.That(result[0].Domain, Is.EqualTo("Motor"));
        Assert.That(result[0].AmountDue, Is.EqualTo(300));
        Assert.That(result[0].DaysOverdue, Is.GreaterThanOrEqualTo(12));
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

        var mockClaimRepo = new Mock<IClaimRepository>();
        mockClaimRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Claim>());
        _mockUnitOfWork.Setup(u => u.Claims).Returns(mockClaimRepo.Object);

        var result = await _financeService.GetPremiumCollectionSummaryAsync("this_month");

        Assert.That(result.TotalCollected, Is.EqualTo(100));
        Assert.That(result.Premiums, Is.EqualTo(100));
        Assert.That(result.ClaimsPaid, Is.EqualTo(0));
        Assert.That(result.NetInflow, Is.EqualTo(100));
        Assert.That(result.SuccessfulPayments, Is.EqualTo(1));
        Assert.That(result.FailedPayments, Is.EqualTo(1));
    }

    [Test]
    public async Task GetPremiumCollectionSummaryAsync_NetInflowSubtractsSettledClaims()
    {
        var month = new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero);
        var payments = new List<PremiumPayment>
        {
            new PremiumPayment { Amount = 20000, Status = PaymentStatus.Paid, CreatedAt = month },
            new PremiumPayment { Amount = 5000, Status = PaymentStatus.Paid, CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero) }
        };
        var claims = new List<Claim>
        {
            new Claim { Status = ClaimStatus.Settled, ClaimAmountApproved = 8000, SettlementDate = new DateTime(2026, 6, 20, 0, 0, 0, DateTimeKind.Utc) },
            new Claim { Status = ClaimStatus.Settled, ClaimAmountApproved = 3000, SettlementDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc) }
        };

        var mockPaymentRepo = new Mock<IPremiumPaymentRepository>();
        mockPaymentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(payments);
        _mockUnitOfWork.Setup(u => u.PremiumPayments).Returns(mockPaymentRepo.Object);

        var mockClaimRepo = new Mock<IClaimRepository>();
        mockClaimRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(claims);
        _mockUnitOfWork.Setup(u => u.Claims).Returns(mockClaimRepo.Object);

        var result = await _financeService.GetPremiumCollectionSummaryAsync("Jun 2026");

        // Only the June payment (20000) and June settled claim (8000) fall in the period.
        Assert.That(result.Premiums, Is.EqualTo(20000));
        Assert.That(result.ClaimsPaid, Is.EqualTo(8000));
        Assert.That(result.NetInflow, Is.EqualTo(12000));
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
        _mockStripeWrapper.Setup(s => s.CreatePaymentIntentAsync(It.IsAny<PaymentIntentCreateOptions>(), It.IsAny<RequestOptions>()))
            .ReturnsAsync(paymentIntent);
        _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

        await _financeService.ProcessClaimPayoutAsync(claimId.ToString(), officerId.ToString());

        Assert.That(claim.Status, Is.EqualTo(ClaimStatus.Settled));
        Assert.That(claim.SettlementDate, Is.Not.Null);
        _mockStripeWrapper.Verify(s => s.CreatePaymentIntentAsync(It.Is<PaymentIntentCreateOptions>(
            o => o.Amount == 50000 && o.Currency == "inr"), It.IsAny<RequestOptions>()), Times.Once);
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

        ClaimStatusHistory? capturedHistory = null;
        mockHistoryRepo.Setup(r => r.AddAsync(It.IsAny<ClaimStatusHistory>()))
            .Callback<ClaimStatusHistory>(h => capturedHistory = h)
            .Returns(Task.CompletedTask);

        await _financeService.MarkClaimFinanciallySettledAsync(claimId.ToString(), officerId.ToString());

        Assert.That(claim.Status, Is.EqualTo(ClaimStatus.Settled));
        Assert.That(claim.SettlementDate, Is.Not.Null);
        Assert.That(capturedHistory, Is.Not.Null);
        Assert.That(capturedHistory!.OldStatus, Is.EqualTo(ClaimStatus.Approved));
        Assert.That(capturedHistory.NewStatus, Is.EqualTo(ClaimStatus.Settled));
        mockHistoryRepo.Verify(r => r.AddAsync(It.IsAny<ClaimStatusHistory>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public void MarkClaimFinanciallySettledAsync_NonApprovedClaim_ThrowsUnprocessableException()
    {
        var claimId = Guid.NewGuid();
        var claim = new Claim { Id = claimId, Status = ClaimStatus.Intimated };

        var mockClaimRepo = new Mock<IClaimRepository>();
        mockClaimRepo.Setup(r => r.GetByIdAsync(claimId)).ReturnsAsync(claim);
        _mockUnitOfWork.Setup(u => u.Claims).Returns(mockClaimRepo.Object);

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.UnprocessableException>(() =>
            _financeService.MarkClaimFinanciallySettledAsync(claimId.ToString(), Guid.NewGuid().ToString()));
        Assert.That(claim.Status, Is.EqualTo(ClaimStatus.Intimated));
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
    public async Task ExportPaymentReportsAsync_FiltersByDateRange()
    {
        var inRange = new PremiumPayment
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Amount = 100,
            Currency = "INR",
            PaymentType = PaymentType.FirstPremium,
            Status = PaymentStatus.Paid,
            CreatedAt = new DateTimeOffset(2026, 3, 15, 0, 0, 0, TimeSpan.Zero)
        };
        var outOfRange = new PremiumPayment
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Amount = 200,
            Currency = "INR",
            PaymentType = PaymentType.FirstPremium,
            Status = PaymentStatus.Paid,
            CreatedAt = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero)
        };

        var mockPaymentRepo = new Mock<IPremiumPaymentRepository>();
        mockPaymentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<PremiumPayment> { inRange, outOfRange });
        _mockUnitOfWork.Setup(u => u.PremiumPayments).Returns(mockPaymentRepo.Object);

        var result = await _financeService.ExportPaymentReportsAsync(
            new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30));

        using var ms = new System.IO.MemoryStream(result);
        using var workbook = new ClosedXML.Excel.XLWorkbook(ms);
        var ws = workbook.Worksheets.First();
        var dataRows = ws.RowsUsed().Count() - 1; // exclude header

        Assert.That(dataRows, Is.EqualTo(1), "Only the in-range payment should be exported.");
        Assert.That(ws.Cell(2, 1).GetString(), Is.EqualTo(inRange.Id.ToString()));
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
        var policyId = Guid.NewGuid();
        var request = new CreatePaymentIntentRequest { PolicyId = policyId };

        var schedule = new PremiumSchedule { Id = scheduleId, PolicyId = policyId, Amount = 200m, Status = PremiumScheduleStatus.Upcoming, InstallmentNumber = 2 };
        var mockScheduleRepo = new Mock<IRepository<PremiumSchedule>>();
        mockScheduleRepo.Setup(r => r.GetByIdAsync(scheduleId)).ReturnsAsync(schedule);
        _mockUnitOfWork.Setup(u => u.PremiumSchedules).Returns(mockScheduleRepo.Object);

        var customerEntity = new SpeedClaim.Api.Models.Customer { Id = customerId, UserId = userId };
        var mockCustomerRepo = new Mock<IRepository<SpeedClaim.Api.Models.Customer>>();
        mockCustomerRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<SpeedClaim.Api.Models.Customer, bool>>>())).ReturnsAsync(customerEntity);
        _mockUnitOfWork.Setup(u => u.Customers).Returns(mockCustomerRepo.Object);

        var mockPolicyRepo = new Mock<IPolicyRepository>();
        mockPolicyRepo.Setup(r => r.GetByIdAsync(policyId)).ReturnsAsync(new Policy { Id = policyId, CustomerId = customerEntity.Id });
        _mockUnitOfWork.Setup(u => u.Policies).Returns(mockPolicyRepo.Object);

        var mockStripeCustomerRepo = new Mock<IRepository<SpeedClaim.Api.Models.StripeCustomer>>();
        mockStripeCustomerRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<SpeedClaim.Api.Models.StripeCustomer, bool>>>()))
            .ReturnsAsync((SpeedClaim.Api.Models.StripeCustomer?)null);
        _mockUnitOfWork.Setup(u => u.StripeCustomers).Returns(mockStripeCustomerRepo.Object);

        var user = new User { Id = userId, Email = "u@test.com", FirstName = "U", LastName = "S" };
        var mockUserRepo = new Mock<IUserRepository>();
        mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockUnitOfWork.Setup(u => u.Users).Returns(mockUserRepo.Object);

        _mockStripeWrapper.Setup(s => s.CreateCustomerAsync(It.IsAny<Stripe.CustomerCreateOptions>(), It.IsAny<RequestOptions>()))
            .ReturnsAsync(new Stripe.Customer { Id = "cus_new_123" });

        var paymentIntent = new Stripe.PaymentIntent { Id = "pi_new_123", ClientSecret = "secret_new" };
        _mockStripeWrapper.Setup(s => s.CreatePaymentIntentAsync(It.IsAny<Stripe.PaymentIntentCreateOptions>(), It.IsAny<RequestOptions>()))
            .ReturnsAsync(paymentIntent);

        var mockPaymentRepo = new Mock<IPremiumPaymentRepository>();
        _mockUnitOfWork.Setup(u => u.PremiumPayments).Returns(mockPaymentRepo.Object);
        _mockConfig.Setup(c => c["Stripe:PublishableKey"]).Returns("pk_test");

        var result = await _financeService.PayPremiumAsync(customerId.ToString(), scheduleId.ToString(), request);

        Assert.That(result.PaymentIntentId, Is.EqualTo("pi_new_123"));
        mockStripeCustomerRepo.Verify(r => r.AddAsync(It.IsAny<SpeedClaim.Api.Models.StripeCustomer>()), Times.Once);
    }

    [Test]
    public async Task PayPremiumAsync_ConcurrentFirstTimePayment_RecoversFromStripeCustomerRaceInsteadOf500ing()
    {
        // Simulates two concurrent PayPremiumAsync calls for the same brand-new customer:
        // both see "no StripeCustomer row" and race to insert one. stripe_customers.user_id
        // is uniquely constrained (same shape as the KYC-record race fixed earlier), so the
        // loser's SaveChanges throws a Postgres unique-violation. The fix must catch that,
        // discard its own (never-persisted) row, and re-fetch the row the winner actually
        // committed — then build the PaymentIntent against THAT customer, not its own.
        var customerId = Guid.NewGuid();
        var scheduleId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        var request = new CreatePaymentIntentRequest { PolicyId = policyId };

        var schedule = new PremiumSchedule { Id = scheduleId, PolicyId = policyId, Amount = 200m, Status = PremiumScheduleStatus.Upcoming, InstallmentNumber = 2 };
        var mockScheduleRepo = new Mock<IRepository<PremiumSchedule>>();
        mockScheduleRepo.Setup(r => r.GetByIdAsync(scheduleId)).ReturnsAsync(schedule);
        _mockUnitOfWork.Setup(u => u.PremiumSchedules).Returns(mockScheduleRepo.Object);

        var customerEntity = new SpeedClaim.Api.Models.Customer { Id = customerId, UserId = userId };
        var mockCustomerRepo = new Mock<IRepository<SpeedClaim.Api.Models.Customer>>();
        mockCustomerRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<SpeedClaim.Api.Models.Customer, bool>>>())).ReturnsAsync(customerEntity);
        _mockUnitOfWork.Setup(u => u.Customers).Returns(mockCustomerRepo.Object);

        var mockPolicyRepo = new Mock<IPolicyRepository>();
        mockPolicyRepo.Setup(r => r.GetByIdAsync(policyId)).ReturnsAsync(new Policy { Id = policyId, CustomerId = customerEntity.Id });
        _mockUnitOfWork.Setup(u => u.Policies).Returns(mockPolicyRepo.Object);

        // The row the OTHER (winning) concurrent request actually committed.
        var winningStripeCustomer = new SpeedClaim.Api.Models.StripeCustomer { UserId = userId, StripeCustomerId = "cus_winner" };
        var mockStripeCustomerRepo = new Mock<IRepository<SpeedClaim.Api.Models.StripeCustomer>>();
        mockStripeCustomerRepo.SetupSequence(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<SpeedClaim.Api.Models.StripeCustomer, bool>>>()))
            .ReturnsAsync((SpeedClaim.Api.Models.StripeCustomer?)null)   // initial check: no row yet
            .ReturnsAsync(winningStripeCustomer);                        // re-fetch after our own insert loses the race
        _mockUnitOfWork.Setup(u => u.StripeCustomers).Returns(mockStripeCustomerRepo.Object);

        var user = new User { Id = userId, Email = "u@test.com", FirstName = "U", LastName = "S" };
        var mockUserRepo = new Mock<IUserRepository>();
        mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockUnitOfWork.Setup(u => u.Users).Returns(mockUserRepo.Object);

        // Our own (losing) attempt still calls Stripe and builds a local row before the DB
        // race is discovered — that's unavoidable, since the race can only be detected once
        // we try to commit it.
        _mockStripeWrapper.Setup(s => s.CreateCustomerAsync(It.IsAny<Stripe.CustomerCreateOptions>(), It.IsAny<RequestOptions>()))
            .ReturnsAsync(new Stripe.Customer { Id = "cus_loser" });

        var paymentIntent = new Stripe.PaymentIntent { Id = "pi_after_race", ClientSecret = "secret_after_race" };
        _mockStripeWrapper.Setup(s => s.CreatePaymentIntentAsync(It.IsAny<Stripe.PaymentIntentCreateOptions>(), It.IsAny<RequestOptions>()))
            .ReturnsAsync(paymentIntent);

        var uniqueViolation = new PostgresException("duplicate key value violates unique constraint \"IX_stripe_customers_user_id\"", "ERROR", "ERROR", "23505");
        _mockUnitOfWork.SetupSequence(u => u.CompleteAsync())
            .ThrowsAsync(new DbUpdateException("update failed", uniqueViolation)) // our own StripeCustomer insert loses the race
            .ReturnsAsync(1); // the later PremiumPayment save succeeds normally

        var mockPaymentRepo = new Mock<IPremiumPaymentRepository>();
        _mockUnitOfWork.Setup(u => u.PremiumPayments).Returns(mockPaymentRepo.Object);
        _mockConfig.Setup(c => c["Stripe:PublishableKey"]).Returns("pk_test");

        var result = await _financeService.PayPremiumAsync(customerId.ToString(), scheduleId.ToString(), request);

        Assert.That(result.PaymentIntentId, Is.EqualTo("pi_after_race"));
        // The PaymentIntent must be attached to the WINNER's Stripe customer, not our own
        // never-persisted one — otherwise the payment would be tied to an orphaned local row.
        _mockStripeWrapper.Verify(s => s.CreatePaymentIntentAsync(
            It.Is<Stripe.PaymentIntentCreateOptions>(o => o.Customer == "cus_winner"), It.IsAny<RequestOptions>()), Times.Once);
        mockStripeCustomerRepo.Verify(r => r.Delete(It.Is<SpeedClaim.Api.Models.StripeCustomer>(sc => sc.StripeCustomerId == "cus_loser")), Times.Once);
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
        _mockUnitOfWork.Verify(u => u.PolicyStatusHistories.AddAsync(It.Is<PolicyStatusHistory>(h =>
            h.PolicyId == policyId &&
            h.OldStatus == PolicyStatus.Pending &&
            h.NewStatus == PolicyStatus.Active &&
            h.Reason == "First premium paid; policy activated.")), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public async Task ReconcilePaymentAsync_AgentPolicy_CreatesPendingCommission()
    {
        var paymentId = Guid.NewGuid();
        var scheduleId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        var agentId = Guid.NewGuid();

        var payment = new PremiumPayment { Id = paymentId, Status = PaymentStatus.Pending, ScheduleId = scheduleId, Amount = 10000m };
        var schedule = new PremiumSchedule { Id = scheduleId, Status = PremiumScheduleStatus.Upcoming, PolicyId = policyId };
        // Already Active so the certificate/email activation branch is skipped — isolate commission logic.
        var policy = new Policy { Id = policyId, Status = PolicyStatus.Active, AgentId = agentId, PolicyNumber = "POL-AG-001" };
        var agent = new Agent { Id = agentId, CommissionRate = 0.10m };

        var mockPaymentRepo = new Mock<IPremiumPaymentRepository>();
        mockPaymentRepo.Setup(r => r.GetByIdAsync(paymentId)).ReturnsAsync(payment);
        _mockUnitOfWork.Setup(u => u.PremiumPayments).Returns(mockPaymentRepo.Object);

        var mockScheduleRepo = new Mock<IRepository<PremiumSchedule>>();
        mockScheduleRepo.Setup(r => r.GetByIdAsync(scheduleId)).ReturnsAsync(schedule);
        _mockUnitOfWork.Setup(u => u.PremiumSchedules).Returns(mockScheduleRepo.Object);

        var mockPolicyRepo = new Mock<IPolicyRepository>();
        mockPolicyRepo.Setup(r => r.GetByIdAsync(policyId)).ReturnsAsync(policy);
        _mockUnitOfWork.Setup(u => u.Policies).Returns(mockPolicyRepo.Object);

        var mockAgentRepo = new Mock<IRepository<Agent>>();
        mockAgentRepo.Setup(r => r.GetByIdAsync(agentId)).ReturnsAsync(agent);
        _mockUnitOfWork.Setup(u => u.Agents).Returns(mockAgentRepo.Object);

        var mockCommissionRepo = new Mock<IRepository<AgentCommission>>();
        mockCommissionRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<AgentCommission, bool>>>()))
            .ReturnsAsync((AgentCommission?)null);
        _mockUnitOfWork.Setup(u => u.AgentCommissions).Returns(mockCommissionRepo.Object);

        _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

        await _financeService.ReconcilePaymentAsync(paymentId.ToString(), Guid.NewGuid().ToString());

        mockCommissionRepo.Verify(r => r.AddAsync(It.Is<AgentCommission>(c =>
            c.AgentId == agentId &&
            c.PolicyId == policyId &&
            c.PremiumPaymentId == paymentId &&
            c.CommissionRate == 0.10m &&
            c.CommissionAmount == 1000m &&
            c.Status == "PENDING")), Times.Once);
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

    // --- Stripe Idempotency Key Tests ---

    [Test]
    public async Task PayPremiumAsync_PassesIdempotencyKeyToStripe()
    {
        var customerId = Guid.NewGuid();
        var scheduleId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        var request = new CreatePaymentIntentRequest { PolicyId = policyId };

        var schedule = new PremiumSchedule
        {
            Id = scheduleId,
            PolicyId = policyId,
            Amount = 100.0m,
            Status = PremiumScheduleStatus.Upcoming,
            InstallmentNumber = 1
        };

        _mockUnitOfWork.Setup(u => u.PremiumSchedules)
            .Returns(Mock.Of<IRepository<PremiumSchedule>>(r =>
                r.GetByIdAsync(scheduleId) == Task.FromResult<PremiumSchedule?>(schedule)));

        var customer = new SpeedClaim.Api.Models.Customer { Id = customerId, UserId = Guid.NewGuid() };
        _mockUnitOfWork.Setup(u => u.Customers)
            .Returns(Mock.Of<IRepository<SpeedClaim.Api.Models.Customer>>(r =>
                r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<SpeedClaim.Api.Models.Customer, bool>>>()) == Task.FromResult<SpeedClaim.Api.Models.Customer?>(customer)));

        _mockUnitOfWork.Setup(u => u.Policies)
            .Returns(Mock.Of<IPolicyRepository>(r =>
                r.GetByIdAsync(policyId) == Task.FromResult<Policy?>(new Policy { Id = policyId, CustomerId = customer.Id })));

        var stripeCustomer = new SpeedClaim.Api.Models.StripeCustomer { UserId = customer.UserId, StripeCustomerId = "cus_test" };
        _mockUnitOfWork.Setup(u => u.StripeCustomers)
            .Returns(Mock.Of<IRepository<SpeedClaim.Api.Models.StripeCustomer>>(r =>
                r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<SpeedClaim.Api.Models.StripeCustomer, bool>>>()) == Task.FromResult<SpeedClaim.Api.Models.StripeCustomer?>(stripeCustomer)));

        var paymentIntent = new PaymentIntent { Id = "pi_idem_test", ClientSecret = "secret_idem" };
        _mockStripeWrapper.Setup(s => s.CreatePaymentIntentAsync(It.IsAny<PaymentIntentCreateOptions>(), It.IsAny<RequestOptions>()))
            .ReturnsAsync(paymentIntent);

        var mockPaymentRepo = new Mock<IPremiumPaymentRepository>();
        _mockUnitOfWork.Setup(u => u.PremiumPayments).Returns(mockPaymentRepo.Object);
        _mockConfig.Setup(c => c["Stripe:PublishableKey"]).Returns("pk_test");

        await _financeService.PayPremiumAsync(customerId.ToString(), scheduleId.ToString(), request);

        _mockStripeWrapper.Verify(s => s.CreatePaymentIntentAsync(
            It.IsAny<PaymentIntentCreateOptions>(),
            It.Is<RequestOptions>(r => r.IdempotencyKey == $"pay-premium-{scheduleId}-inr")),
            Times.Once);
    }

    [Test]
    public async Task ProcessClaimPayoutAsync_PassesIdempotencyKeyToStripe()
    {
        var claimId = Guid.NewGuid();
        var officerId = Guid.NewGuid();
        var claim = new Claim
        {
            Id = claimId,
            ClaimNumber = "CLM-IDEM-001",
            Status = ClaimStatus.Approved,
            ClaimAmountApproved = 500m
        };

        var mockClaimRepo = new Mock<IClaimRepository>();
        mockClaimRepo.Setup(r => r.GetByIdAsync(claimId)).ReturnsAsync(claim);
        _mockUnitOfWork.Setup(u => u.Claims).Returns(mockClaimRepo.Object);

        var mockHistoryRepo = new Mock<IRepository<ClaimStatusHistory>>();
        _mockUnitOfWork.Setup(u => u.ClaimStatusHistories).Returns(mockHistoryRepo.Object);
        _mockUnitOfWork.Setup(u => u.AuditLogs).Returns(new Mock<IRepository<AuditLog>>().Object);

        var paymentIntent = new PaymentIntent { Id = "pi_payout_idem" };
        _mockStripeWrapper.Setup(s => s.CreatePaymentIntentAsync(It.IsAny<PaymentIntentCreateOptions>(), It.IsAny<RequestOptions>()))
            .ReturnsAsync(paymentIntent);

        await _financeService.ProcessClaimPayoutAsync(claimId.ToString(), officerId.ToString());

        _mockStripeWrapper.Verify(s => s.CreatePaymentIntentAsync(
            It.IsAny<PaymentIntentCreateOptions>(),
            It.Is<RequestOptions>(r => r.IdempotencyKey == $"claim-payout-{claimId}-inr")),
            Times.Once);
    }

    // --- Currency / retry-safety / failure-handling tests ---

    // Arranges a payable schedule + owning customer/policy/stripe-customer so PayPremiumAsync
    // reaches intent creation.
    private (Guid customerId, Guid scheduleId, Guid policyId, Mock<IPremiumPaymentRepository> paymentRepo) ArrangePayableSchedule(
        PremiumPayment? existingPayment = null)
    {
        var customerId = Guid.NewGuid();
        var scheduleId = Guid.NewGuid();
        var policyId = Guid.NewGuid();

        var schedule = new PremiumSchedule
        {
            Id = scheduleId,
            PolicyId = policyId,
            Amount = 2500.50m,
            Status = PremiumScheduleStatus.Due,
            InstallmentNumber = 1
        };
        _mockUnitOfWork.Setup(u => u.PremiumSchedules)
            .Returns(Mock.Of<IRepository<PremiumSchedule>>(r =>
                r.GetByIdAsync(scheduleId) == Task.FromResult<PremiumSchedule?>(schedule)));

        var customer = new SpeedClaim.Api.Models.Customer { Id = customerId, UserId = Guid.NewGuid() };
        _mockUnitOfWork.Setup(u => u.Customers)
            .Returns(Mock.Of<IRepository<SpeedClaim.Api.Models.Customer>>(r =>
                r.FirstOrDefaultAsync(It.IsAny<Expression<Func<SpeedClaim.Api.Models.Customer, bool>>>()) == Task.FromResult<SpeedClaim.Api.Models.Customer?>(customer)));

        _mockUnitOfWork.Setup(u => u.Policies)
            .Returns(Mock.Of<IPolicyRepository>(r =>
                r.GetByIdAsync(policyId) == Task.FromResult<Policy?>(new Policy { Id = policyId, CustomerId = customerId })));

        var stripeCustomer = new SpeedClaim.Api.Models.StripeCustomer { UserId = customer.UserId, StripeCustomerId = "cus_test" };
        _mockUnitOfWork.Setup(u => u.StripeCustomers)
            .Returns(Mock.Of<IRepository<SpeedClaim.Api.Models.StripeCustomer>>(r =>
                r.FirstOrDefaultAsync(It.IsAny<Expression<Func<SpeedClaim.Api.Models.StripeCustomer, bool>>>()) == Task.FromResult<SpeedClaim.Api.Models.StripeCustomer?>(stripeCustomer)));

        var mockPaymentRepo = new Mock<IPremiumPaymentRepository>();
        mockPaymentRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<PremiumPayment, bool>>>()))
            .ReturnsAsync(existingPayment);
        _mockUnitOfWork.Setup(u => u.PremiumPayments).Returns(mockPaymentRepo.Object);

        _mockConfig.Setup(c => c["Stripe:PublishableKey"]).Returns("pk_test");
        return (customerId, scheduleId, policyId, mockPaymentRepo);
    }

    [Test]
    public async Task PayPremiumAsync_CreatesIntentInInrPaise()
    {
        var (customerId, scheduleId, policyId, _) = ArrangePayableSchedule();
        _mockStripeWrapper.Setup(s => s.CreatePaymentIntentAsync(It.IsAny<PaymentIntentCreateOptions>(), It.IsAny<RequestOptions>()))
            .ReturnsAsync(new PaymentIntent { Id = "pi_inr", ClientSecret = "secret_inr" });

        await _financeService.PayPremiumAsync(customerId.ToString(), scheduleId.ToString(),
            new CreatePaymentIntentRequest { PolicyId = policyId });

        _mockStripeWrapper.Verify(s => s.CreatePaymentIntentAsync(It.Is<PaymentIntentCreateOptions>(
            o => o.Amount == 250050 && o.Currency == "inr"), It.IsAny<RequestOptions>()), Times.Once);
    }

    [Test]
    public void PayPremiumAsync_ExistingIntentAlreadySucceeded_ReconcilesAndThrowsConflict()
    {
        var existingPayment = new PremiumPayment
        {
            Id = Guid.NewGuid(),
            Status = PaymentStatus.Pending,
            StripePaymentIntentId = "pi_done",
            ScheduleId = Guid.NewGuid()
        };
        var (customerId, scheduleId, policyId, paymentRepo) = ArrangePayableSchedule(existingPayment);
        paymentRepo.Setup(r => r.GetByIntentWithScheduleAsync("pi_done")).ReturnsAsync(existingPayment);
        paymentRepo.Setup(r => r.GetByIdAsync(existingPayment.Id)).ReturnsAsync(existingPayment);
        _mockStripeWrapper.Setup(s => s.GetPaymentIntentAsync("pi_done"))
            .ReturnsAsync(new PaymentIntent { Id = "pi_done", Status = "succeeded" });

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ConflictException>(() =>
            _financeService.PayPremiumAsync(customerId.ToString(), scheduleId.ToString(),
                new CreatePaymentIntentRequest { PolicyId = policyId }));

        // The stranded successful charge must be reconciled, and no second intent created
        Assert.That(existingPayment.Status, Is.EqualTo(PaymentStatus.Paid));
        _mockStripeWrapper.Verify(s => s.CreatePaymentIntentAsync(It.IsAny<PaymentIntentCreateOptions>(), It.IsAny<RequestOptions>()), Times.Never);
    }

    [Test]
    public async Task PayPremiumAsync_StaleIntentLookupFails_CreatesFreshIntent()
    {
        var existingPayment = new PremiumPayment
        {
            Id = Guid.NewGuid(),
            Status = PaymentStatus.Failed,
            StripePaymentIntentId = "pi_stale",
            Currency = "USD"
        };
        var (customerId, scheduleId, policyId, _) = ArrangePayableSchedule(existingPayment);
        _mockStripeWrapper.Setup(s => s.GetPaymentIntentAsync("pi_stale"))
            .ThrowsAsync(new StripeException("No such payment_intent"));
        _mockStripeWrapper.Setup(s => s.CreatePaymentIntentAsync(It.IsAny<PaymentIntentCreateOptions>(), It.IsAny<RequestOptions>()))
            .ReturnsAsync(new PaymentIntent { Id = "pi_fresh", ClientSecret = "secret_fresh" });

        var result = await _financeService.PayPremiumAsync(customerId.ToString(), scheduleId.ToString(),
            new CreatePaymentIntentRequest { PolicyId = policyId });

        Assert.That(result.PaymentIntentId, Is.EqualTo("pi_fresh"));
        // The reused row is re-pointed at the fresh intent and reset for the new attempt
        Assert.That(existingPayment.StripePaymentIntentId, Is.EqualTo("pi_fresh"));
        Assert.That(existingPayment.Status, Is.EqualTo(PaymentStatus.Pending));
        Assert.That(existingPayment.Currency, Is.EqualTo("INR"));
    }

    [Test]
    public async Task MarkPaymentFailedByStripeIntentAsync_PendingPayment_MarksFailed()
    {
        var payment = new PremiumPayment { Id = Guid.NewGuid(), Status = PaymentStatus.Pending, StripePaymentIntentId = "pi_fail" };
        var mockPaymentRepo = new Mock<IPremiumPaymentRepository>();
        mockPaymentRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<PremiumPayment, bool>>>()))
            .ReturnsAsync(payment);
        _mockUnitOfWork.Setup(u => u.PremiumPayments).Returns(mockPaymentRepo.Object);

        await _financeService.MarkPaymentFailedByStripeIntentAsync("pi_fail");

        Assert.That(payment.Status, Is.EqualTo(PaymentStatus.Failed));
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public async Task MarkPaymentFailedByStripeIntentAsync_PaidPayment_IsNotDowngraded()
    {
        var payment = new PremiumPayment { Id = Guid.NewGuid(), Status = PaymentStatus.Paid, StripePaymentIntentId = "pi_paid" };
        var mockPaymentRepo = new Mock<IPremiumPaymentRepository>();
        mockPaymentRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<PremiumPayment, bool>>>()))
            .ReturnsAsync(payment);
        _mockUnitOfWork.Setup(u => u.PremiumPayments).Returns(mockPaymentRepo.Object);

        await _financeService.MarkPaymentFailedByStripeIntentAsync("pi_paid");

        Assert.That(payment.Status, Is.EqualTo(PaymentStatus.Paid));
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Never);
    }

    [Test]
    public void ProcessRefundAsync_PendingPayment_ThrowsUnprocessable()
    {
        var paymentId = Guid.NewGuid();
        var payment = new PremiumPayment { Id = paymentId, Status = PaymentStatus.Pending };
        var mockPaymentRepo = new Mock<IPremiumPaymentRepository>();
        mockPaymentRepo.Setup(r => r.GetByIdAsync(paymentId)).ReturnsAsync(payment);
        _mockUnitOfWork.Setup(u => u.PremiumPayments).Returns(mockPaymentRepo.Object);

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.UnprocessableException>(() =>
            _financeService.ProcessRefundAsync(paymentId.ToString(), Guid.NewGuid().ToString()));
    }

    [Test]
    public void ProcessRefundAsync_AlreadyRefunded_ThrowsConflict()
    {
        var paymentId = Guid.NewGuid();
        var payment = new PremiumPayment { Id = paymentId, Status = PaymentStatus.Refunded };
        var mockPaymentRepo = new Mock<IPremiumPaymentRepository>();
        mockPaymentRepo.Setup(r => r.GetByIdAsync(paymentId)).ReturnsAsync(payment);
        _mockUnitOfWork.Setup(u => u.PremiumPayments).Returns(mockPaymentRepo.Object);

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ConflictException>(() =>
            _financeService.ProcessRefundAsync(paymentId.ToString(), Guid.NewGuid().ToString()));
    }

    [Test]
    public async Task ProcessRefundAsync_PaidWithStripeIntent_CreatesStripeRefund()
    {
        var paymentId = Guid.NewGuid();
        var payment = new PremiumPayment { Id = paymentId, Status = PaymentStatus.Paid, StripePaymentIntentId = "pi_refund_me" };
        var mockPaymentRepo = new Mock<IPremiumPaymentRepository>();
        mockPaymentRepo.Setup(r => r.GetByIdAsync(paymentId)).ReturnsAsync(payment);
        _mockUnitOfWork.Setup(u => u.PremiumPayments).Returns(mockPaymentRepo.Object);
        _mockStripeWrapper.Setup(s => s.CreateRefundAsync(It.IsAny<RefundCreateOptions>(), It.IsAny<RequestOptions>()))
            .ReturnsAsync(new Refund { Id = "re_test" });

        await _financeService.ProcessRefundAsync(paymentId.ToString(), Guid.NewGuid().ToString());

        Assert.That(payment.Status, Is.EqualTo(PaymentStatus.Refunded));
        _mockStripeWrapper.Verify(s => s.CreateRefundAsync(
            It.Is<RefundCreateOptions>(o => o.PaymentIntent == "pi_refund_me"),
            It.Is<RequestOptions>(r => r.IdempotencyKey == $"refund-{paymentId}")), Times.Once);
    }

    [Test]
    public void ProcessRefundAsync_StripeRefundFails_DoesNotMarkRefunded()
    {
        var paymentId = Guid.NewGuid();
        var payment = new PremiumPayment { Id = paymentId, Status = PaymentStatus.Paid, StripePaymentIntentId = "pi_cannot_refund" };
        var mockPaymentRepo = new Mock<IPremiumPaymentRepository>();
        mockPaymentRepo.Setup(r => r.GetByIdAsync(paymentId)).ReturnsAsync(payment);
        _mockUnitOfWork.Setup(u => u.PremiumPayments).Returns(mockPaymentRepo.Object);
        _mockStripeWrapper.Setup(s => s.CreateRefundAsync(It.IsAny<RefundCreateOptions>(), It.IsAny<RequestOptions>()))
            .ThrowsAsync(new StripeException("refund blocked"));

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.UnprocessableException>(() =>
            _financeService.ProcessRefundAsync(paymentId.ToString(), Guid.NewGuid().ToString()));
        Assert.That(payment.Status, Is.EqualTo(PaymentStatus.Paid));
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Never);
    }

    [Test]
    public async Task ReconcilePaymentAsync_EmailFailure_StillCommitsReconciliation()
    {
        var paymentId = Guid.NewGuid();
        var scheduleId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var payment = new PremiumPayment { Id = paymentId, Status = PaymentStatus.Pending, ScheduleId = scheduleId, Amount = 100 };
        var schedule = new PremiumSchedule { Id = scheduleId, PolicyId = policyId, Status = PremiumScheduleStatus.Due, InstallmentNumber = 1 };
        var policy = new Policy { Id = policyId, CustomerId = customerId, Status = PolicyStatus.Pending, PolicyNumber = "POL-1" };

        var mockPaymentRepo = new Mock<IPremiumPaymentRepository>();
        mockPaymentRepo.Setup(r => r.GetByIdAsync(paymentId)).ReturnsAsync(payment);
        _mockUnitOfWork.Setup(u => u.PremiumPayments).Returns(mockPaymentRepo.Object);
        _mockUnitOfWork.Setup(u => u.PremiumSchedules)
            .Returns(Mock.Of<IRepository<PremiumSchedule>>(r => r.GetByIdAsync(scheduleId) == Task.FromResult<PremiumSchedule?>(schedule)));
        _mockUnitOfWork.Setup(u => u.Policies)
            .Returns(Mock.Of<IPolicyRepository>(r => r.GetByIdAsync(policyId) == Task.FromResult<Policy?>(policy)));
        _mockUnitOfWork.Setup(u => u.Customers)
            .Returns(Mock.Of<IRepository<SpeedClaim.Api.Models.Customer>>(r =>
                r.GetByIdAsync(customerId) == Task.FromResult<SpeedClaim.Api.Models.Customer?>(new SpeedClaim.Api.Models.Customer { Id = customerId, UserId = userId })));
        _mockUnitOfWork.Setup(u => u.Users)
            .Returns(Mock.Of<IUserRepository>(r =>
                r.GetByIdAsync(userId) == Task.FromResult<User?>(new User { Id = userId, Email = "c@test.com", FirstName = "C", LastName = "T" })));
        _mockEmailService.Setup(e => e.SendTemplatedEmailAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<string>(), It.IsAny<EmailAttachment?>()))
            .ThrowsAsync(new InvalidOperationException("SMTP down"));

        // Must NOT throw — the charge already happened; emails are best-effort
        await _financeService.ReconcilePaymentAsync(paymentId.ToString(), Guid.NewGuid().ToString());

        Assert.That(payment.Status, Is.EqualTo(PaymentStatus.Paid));
        Assert.That(policy.Status, Is.EqualTo(PolicyStatus.Active));
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.AtLeastOnce);
    }
}
