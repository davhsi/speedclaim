using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Moq;
using SpeedClaim.Api.Dtos.Payments;
using SpeedClaim.Api.Models;
using SpeedClaim.Api.Services;
using SpeedClaim.Api.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace SpeedClaim.Tests.Services;

[TestFixture]
public class PaymentServiceTests
{
    private Mock<IUnitOfWork> _mockUnitOfWork;
    private Mock<IPolicyRepository> _mockPolicyRepo;
    private Mock<IPaymentTransactionRepository> _mockPaymentRepo;
    private Mock<IUserRepository> _mockUserRepo;
    private Mock<IConfiguration> _mockConfig;
    private Mock<IEmailService> _mockEmailService;
    private PaymentService _paymentService;

    [SetUp]
    public void Setup()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockPolicyRepo = new Mock<IPolicyRepository>();
        _mockPaymentRepo = new Mock<IPaymentTransactionRepository>();
        _mockUserRepo = new Mock<IUserRepository>();

        _mockUnitOfWork.Setup(u => u.Policies).Returns(_mockPolicyRepo.Object);
        _mockUnitOfWork.Setup(u => u.PaymentTransactions).Returns(_mockPaymentRepo.Object);
        _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepo.Object);

        _mockConfig = new Mock<IConfiguration>();
        var configSection = new Mock<IConfigurationSection>();
        configSection.Setup(x => x["PublishableKey"]).Returns("pk_test_123");
        configSection.Setup(x => x["SecretKey"]).Returns("sk_test_123");
        configSection.Setup(x => x["WebhookSecret"]).Returns("whsec_123");
        _mockConfig.Setup(c => c.GetSection("Stripe")).Returns(configSection.Object);

        _mockEmailService = new Mock<IEmailService>();

        _paymentService = new PaymentService(_mockUnitOfWork.Object, _mockConfig.Object, _mockEmailService.Object);
    }

    // Stripe interactions are hard to test without actually mocking Stripe's API via dependency injection.
    // In our service we instantiate PaymentIntentService directly: `var service = new PaymentIntentService();`
    // which makes it difficult to unit test without external calls.
    // For unit testing purposes to pass the refactoring, we focus on the validation rules.

    [Test]
    public void CreatePaymentIntentAsync_PolicyNotFound_ThrowsArgumentException()
    {
        var request = new CreatePaymentIntentRequest { PolicyId = Guid.NewGuid() };
        _mockPolicyRepo.Setup(r => r.SingleOrDefaultAsync(It.IsAny<Expression<Func<Policy, bool>>>())).ReturnsAsync((Policy?)null);

        var ex = Assert.ThrowsAsync<ArgumentException>(async () => await _paymentService.CreatePaymentIntentAsync(Guid.NewGuid(), request));
        Assert.That(ex.Message, Does.Contain("not found"));
    }

    [Test]
    public void CreatePaymentIntentAsync_PolicyNotPending_ThrowsArgumentException()
    {
        var policy = new HealthPolicy { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Status = "ACTIVE" };
        var request = new CreatePaymentIntentRequest { PolicyId = policy.Id };
        
        _mockPolicyRepo.Setup(r => r.SingleOrDefaultAsync(It.IsAny<Expression<Func<Policy, bool>>>())).ReturnsAsync(policy);

        var ex = Assert.ThrowsAsync<ArgumentException>(async () => await _paymentService.CreatePaymentIntentAsync(policy.UserId, request));
        Assert.That(ex.Message, Does.Contain("PENDING policies"));
    }
}
