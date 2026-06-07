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
    private Mock<IStripeWrapper> _mockStripeWrapper;
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
        _mockStripeWrapper = new Mock<IStripeWrapper>();

        _paymentService = new PaymentService(_mockUnitOfWork.Object, _mockConfig.Object, _mockEmailService.Object, _mockStripeWrapper.Object);
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

    [Test]
    public async Task CreatePaymentIntentAsync_ValidPolicy_CreatesIntentAndTransaction()
    {
        var policy = new HealthPolicy { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Status = "PENDING", PremiumAmount = 1000m };
        var request = new CreatePaymentIntentRequest { PolicyId = policy.Id };
        
        _mockPolicyRepo.Setup(r => r.SingleOrDefaultAsync(It.IsAny<Expression<Func<Policy, bool>>>())).ReturnsAsync(policy);
        
        var paymentIntent = new Stripe.PaymentIntent { Id = "pi_123", ClientSecret = "secret_123" };
        _mockStripeWrapper.Setup(s => s.CreatePaymentIntentAsync(It.IsAny<Stripe.PaymentIntentCreateOptions>())).ReturnsAsync(paymentIntent);

        var result = await _paymentService.CreatePaymentIntentAsync(policy.UserId, request);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.ClientSecret, Is.EqualTo("secret_123"));
        Assert.That(result.PaymentIntentId, Is.EqualTo("pi_123"));
        _mockPaymentRepo.Verify(r => r.AddAsync(It.IsAny<PaymentTransaction>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public void ProcessWebhookAsync_InvalidSignature_ThrowsException()
    {
        _mockStripeWrapper.Setup(s => s.ConstructEvent(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new Stripe.StripeException("Invalid signature"));

        var ex = Assert.ThrowsAsync<Exception>(async () => await _paymentService.ProcessWebhookAsync("{}", "sig"));
        Assert.That(ex.Message, Does.Contain("signature verification failed"));
    }

    [Test]
    public async Task ProcessWebhookAsync_AlreadyProcessed_Discards()
    {
        var stripeEvent = new Stripe.Event { Id = "evt_123", Type = Stripe.EventTypes.PaymentIntentSucceeded };
        _mockStripeWrapper.Setup(s => s.ConstructEvent(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(stripeEvent);

        _mockPaymentRepo.Setup(r => r.SingleOrDefaultAsync(It.IsAny<Expression<Func<PaymentTransaction, bool>>>()))
            .ReturnsAsync(new PaymentTransaction { Id = Guid.NewGuid() }); // Already processed

        await _paymentService.ProcessWebhookAsync("{}", "sig");

        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Never);
    }

    [Test]
    public async Task ProcessWebhookAsync_PaymentSucceeded_UpdatesTransactionAndPolicy()
    {
        var paymentIntent = new Stripe.PaymentIntent { Id = "pi_123" };
        var stripeEvent = new Stripe.Event { Id = "evt_123", Type = Stripe.EventTypes.PaymentIntentSucceeded, Data = new Stripe.EventData { Object = paymentIntent } };
        _mockStripeWrapper.Setup(s => s.ConstructEvent(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(stripeEvent);

        _mockPaymentRepo.Setup(r => r.SingleOrDefaultAsync(It.IsAny<Expression<Func<PaymentTransaction, bool>>>())).ReturnsAsync((PaymentTransaction?)null); // Not processed
        
        var policy = new HealthPolicy { Id = Guid.NewGuid(), Status = "PENDING", UserId = Guid.NewGuid(), PolicyNumber = "P1" };
        var transaction = new PaymentTransaction { Id = Guid.NewGuid(), Status = "REQUIRES_PAYMENT", Policy = policy };
        
        _mockPaymentRepo.Setup(r => r.GetByIntentWithPolicyAsync("pi_123")).ReturnsAsync(transaction);
        _mockUserRepo.Setup(r => r.GetByIdAsync(policy.UserId)).ReturnsAsync(new User { Email = "test@example.com", FirstName = "Test", LastName = "User", Salutation = "Mr." });

        await _paymentService.ProcessWebhookAsync("{}", "sig");

        Assert.That(transaction.Status, Is.EqualTo("SUCCEEDED"));
        Assert.That(policy.Status, Is.EqualTo("ACTIVE"));
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
        _mockEmailService.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task ProcessWebhookAsync_PaymentFailed_UpdatesTransactionStatus()
    {
        var paymentIntent = new Stripe.PaymentIntent { Id = "pi_123" };
        var stripeEvent = new Stripe.Event { Id = "evt_123", Type = Stripe.EventTypes.PaymentIntentPaymentFailed, Data = new Stripe.EventData { Object = paymentIntent } };
        _mockStripeWrapper.Setup(s => s.ConstructEvent(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(stripeEvent);

        _mockPaymentRepo.Setup(r => r.SingleOrDefaultAsync(It.IsAny<Expression<Func<PaymentTransaction, bool>>>()))
            .ReturnsAsync((PaymentTransaction?)null); // First call to idempotency check

        // Second call for the failed transaction
        var transaction = new PaymentTransaction { Id = Guid.NewGuid(), Status = "REQUIRES_PAYMENT" };
        _mockPaymentRepo.SetupSequence(r => r.SingleOrDefaultAsync(It.IsAny<Expression<Func<PaymentTransaction, bool>>>()))
            .ReturnsAsync((PaymentTransaction?)null) // Idempotency
            .ReturnsAsync(transaction); // Get transaction

        await _paymentService.ProcessWebhookAsync("{}", "sig");

        Assert.That(transaction.Status, Is.EqualTo("FAILED"));
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public async Task ProcessWebhookAsync_PaymentSucceeded_TransactionNull_DoesNothing()
    {
        var paymentIntent = new Stripe.PaymentIntent { Id = "pi_123" };
        var stripeEvent = new Stripe.Event { Id = "evt_123", Type = Stripe.EventTypes.PaymentIntentSucceeded, Data = new Stripe.EventData { Object = paymentIntent } };
        _mockStripeWrapper.Setup(s => s.ConstructEvent(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(stripeEvent);

        _mockPaymentRepo.Setup(r => r.GetByIntentWithPolicyAsync("pi_123")).ReturnsAsync((PaymentTransaction?)null);

        await _paymentService.ProcessWebhookAsync("{}", "sig");

        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Never);
    }

    [Test]
    public async Task ProcessWebhookAsync_PaymentSucceeded_PolicyNotPending_DoesNotActivatePolicy()
    {
        var paymentIntent = new Stripe.PaymentIntent { Id = "pi_123" };
        var stripeEvent = new Stripe.Event { Id = "evt_123", Type = Stripe.EventTypes.PaymentIntentSucceeded, Data = new Stripe.EventData { Object = paymentIntent } };
        _mockStripeWrapper.Setup(s => s.ConstructEvent(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(stripeEvent);

        var policy = new HealthPolicy { Id = Guid.NewGuid(), Status = "ACTIVE", UserId = Guid.NewGuid() };
        var transaction = new PaymentTransaction { Id = Guid.NewGuid(), Status = "REQUIRES_PAYMENT", Policy = policy };
        
        _mockPaymentRepo.Setup(r => r.GetByIntentWithPolicyAsync("pi_123")).ReturnsAsync(transaction);

        await _paymentService.ProcessWebhookAsync("{}", "sig");

        Assert.That(transaction.Status, Is.EqualTo("SUCCEEDED"));
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
        _mockEmailService.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task ProcessWebhookAsync_PaymentFailed_TransactionNull_DoesNothing()
    {
        var paymentIntent = new Stripe.PaymentIntent { Id = "pi_123" };
        var stripeEvent = new Stripe.Event { Id = "evt_123", Type = Stripe.EventTypes.PaymentIntentPaymentFailed, Data = new Stripe.EventData { Object = paymentIntent } };
        _mockStripeWrapper.Setup(s => s.ConstructEvent(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(stripeEvent);

        _mockPaymentRepo.Setup(r => r.SingleOrDefaultAsync(It.IsAny<Expression<Func<PaymentTransaction, bool>>>()))
            .ReturnsAsync((PaymentTransaction?)null); // First call to idempotency check

        _mockPaymentRepo.SetupSequence(r => r.SingleOrDefaultAsync(It.IsAny<Expression<Func<PaymentTransaction, bool>>>()))
            .ReturnsAsync((PaymentTransaction?)null) // Idempotency
            .ReturnsAsync((PaymentTransaction?)null); // Get transaction

        await _paymentService.ProcessWebhookAsync("{}", "sig");

        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Never);
    }
}
