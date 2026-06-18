using System;
using System.IO;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using SpeedClaim.Api.Controllers;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;
using Stripe;

namespace SpeedClaim.Tests.Controllers;

[TestFixture]
public class PaymentsControllerWebhookTests
{
    private Mock<IFinanceService> _mockFinanceService = null!;
    private Mock<IStripeWrapper> _mockStripeWrapper = null!;
    private Mock<IConfiguration> _mockConfig = null!;
    private Mock<IUnitOfWork> _mockUnitOfWork = null!;
    private Mock<IRepository<ProcessedWebhookEvent>> _mockWebhookRepo = null!;
    private PaymentsController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _mockFinanceService = new Mock<IFinanceService>();
        _mockStripeWrapper = new Mock<IStripeWrapper>();
        _mockConfig = new Mock<IConfiguration>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockWebhookRepo = new Mock<IRepository<ProcessedWebhookEvent>>();

        _mockUnitOfWork.Setup(u => u.ProcessedWebhookEvents).Returns(_mockWebhookRepo.Object);
        _mockConfig.Setup(c => c["Stripe:WebhookSecret"]).Returns("whsec_test");

        _controller = new PaymentsController(
            _mockFinanceService.Object,
            _mockStripeWrapper.Object,
            _mockConfig.Object,
            _mockUnitOfWork.Object);
    }

    private void SetupRequestBody(string json)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(json));
        httpContext.Request.Headers["Stripe-Signature"] = "sig_test";
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    [Test]
    public async Task StripeWebhook_InvalidSignature_ReturnsBadRequest()
    {
        SetupRequestBody("{}");
        _mockStripeWrapper.Setup(s => s.ConstructEvent(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new StripeException("Invalid signature"));

        var result = await _controller.StripeWebhook();

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task StripeWebhook_DuplicateEvent_ReturnsOkWithoutProcessing()
    {
        SetupRequestBody("{}");

        var stripeEvent = new Event { Id = "evt_duplicate", Type = "payment_intent.succeeded" };
        _mockStripeWrapper.Setup(s => s.ConstructEvent(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(stripeEvent);

        _mockWebhookRepo.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<ProcessedWebhookEvent, bool>>>()))
            .ReturnsAsync(true);

        var result = await _controller.StripeWebhook();

        Assert.That(result, Is.TypeOf<OkResult>());
        _mockFinanceService.Verify(
            f => f.ReconcileByStripeIntentAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
        _mockWebhookRepo.Verify(r => r.AddAsync(It.IsAny<ProcessedWebhookEvent>()), Times.Never);
    }

    [Test]
    public async Task StripeWebhook_NewEvent_ProcessesAndStoresEventId()
    {
        SetupRequestBody("{}");

        var stripeEvent = new Event
        {
            Id = "evt_new_123",
            Type = "payment_intent.succeeded",
            Data = new EventData { Object = new PaymentIntent { Id = "pi_123", LatestChargeId = "ch_123" } }
        };
        _mockStripeWrapper.Setup(s => s.ConstructEvent(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(stripeEvent);

        _mockWebhookRepo.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<ProcessedWebhookEvent, bool>>>()))
            .ReturnsAsync(false);

        var result = await _controller.StripeWebhook();

        Assert.That(result, Is.TypeOf<OkResult>());
        _mockFinanceService.Verify(
            f => f.ReconcileByStripeIntentAsync("pi_123", "ch_123"),
            Times.Once);
        _mockWebhookRepo.Verify(
            r => r.AddAsync(It.Is<ProcessedWebhookEvent>(e =>
                e.StripeEventId == "evt_new_123" && e.EventType == "payment_intent.succeeded")),
            Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public async Task StripeWebhook_UnhandledEventType_StoresButDoesNotProcess()
    {
        SetupRequestBody("{}");

        var stripeEvent = new Event { Id = "evt_other", Type = "charge.refunded" };
        _mockStripeWrapper.Setup(s => s.ConstructEvent(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(stripeEvent);

        _mockWebhookRepo.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<ProcessedWebhookEvent, bool>>>()))
            .ReturnsAsync(false);

        var result = await _controller.StripeWebhook();

        Assert.That(result, Is.TypeOf<OkResult>());
        _mockFinanceService.Verify(
            f => f.ReconcileByStripeIntentAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
        _mockWebhookRepo.Verify(
            r => r.AddAsync(It.Is<ProcessedWebhookEvent>(e => e.StripeEventId == "evt_other")),
            Times.Once);
    }
}
