using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using MailKit.Net.Smtp;
using MimeKit;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;
using SpeedClaim.Api.Services;

namespace SpeedClaim.Tests.Services;

[TestFixture]
public class EmailServiceTests
{
    private Mock<ISmtpClientFactory> _mockSmtpClientFactory;
    private Mock<ISmtpClient> _mockSmtpClient;
    private Mock<ILogger<EmailService>> _mockLogger;
    private Mock<IUnitOfWork> _mockUnitOfWork;
    private EmailService _emailService;

    private static IConfiguration BuildTestConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SmtpSettings:SenderName"] = "SpeedClaim",
                ["SmtpSettings:SenderEmail"] = "test@test.com",
                ["SmtpSettings:Host"] = "smtp.test.com",
                ["SmtpSettings:Port"] = "587",
                ["SmtpSettings:AppPassword"] = "pass",
                ["FrontendUrl"] = "http://localhost:4200"
            })
            .Build();

    [SetUp]
    public void Setup()
    {
        _mockSmtpClientFactory = new Mock<ISmtpClientFactory>();
        _mockSmtpClient = new Mock<ISmtpClient>();
        _mockLogger = new Mock<ILogger<EmailService>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>() { DefaultValue = DefaultValue.Mock };

        _mockSmtpClientFactory.Setup(f => f.CreateClient()).Returns(_mockSmtpClient.Object);

        _emailService = new EmailService(
            _mockSmtpClientFactory.Object,
            BuildTestConfig(),
            _mockLogger.Object,
            _mockUnitOfWork.Object);
    }

    [Test]
    public async Task SendEmailAsync_Success_LogsToDatabase()
    {
        // Act
        await _emailService.SendEmailAsync("recipient@test.com", "Subject", "Body");

        // Assert
        _mockSmtpClient.Verify(c => c.ConnectAsync("smtp.test.com", 587, MailKit.Security.SecureSocketOptions.StartTls, It.IsAny<CancellationToken>()), Times.Once);
        _mockSmtpClient.Verify(c => c.AuthenticateAsync("test@test.com", "pass", It.IsAny<CancellationToken>()), Times.Once);
        _mockSmtpClient.Verify(c => c.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<MailKit.ITransferProgress>()), Times.Once);
        _mockSmtpClient.Verify(c => c.DisconnectAsync(true, It.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.Verify(u => u.EmailLogs.AddAsync(It.Is<EmailLog>(l => l.RecipientEmail == "recipient@test.com" && l.Status == "Sent")), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public async Task SendEmailAsync_WithAttachment_SendsMultipartMessage()
    {
        MimeMessage? sentMessage = null;
        _mockSmtpClient
            .Setup(c => c.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<MailKit.ITransferProgress>()))
            .Callback<MimeMessage, CancellationToken, MailKit.ITransferProgress?>((message, _, _) => sentMessage = message)
            .ReturnsAsync("queued");

        var attachment = new EmailAttachment("policy.pdf", "application/pdf", new byte[] { 1, 2, 3 });

        await _emailService.SendEmailAsync("recipient@test.com", "Subject", "<p>Body</p>", attachment);

        Assert.That(sentMessage, Is.Not.Null);
        Assert.That(sentMessage!.Body, Is.TypeOf<Multipart>());
        var multipart = (Multipart)sentMessage.Body;
        Assert.That(multipart.OfType<MimePart>().Any(p => p.FileName == "policy.pdf"), Is.True);
    }

    [Test]
    public void SendEmailAsync_Failure_LogsFailedStatusAndThrows()
    {
        // Arrange
        _mockSmtpClient.Setup(c => c.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<MailKit.ITransferProgress>()))
            .ThrowsAsync(new InvalidOperationException("SMTP Error"));

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(() => _emailService.SendEmailAsync("recipient@test.com", "Subject", "Body"));

        _mockUnitOfWork.Verify(u => u.EmailLogs.AddAsync(It.Is<EmailLog>(l => l.RecipientEmail == "recipient@test.com" && l.Status == "Failed")), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public async Task SendEmailVerificationAsync_CallsSendEmail()
    {
        var mockTemplateRepo = new Mock<IRepository<EmailTemplate>>();
        mockTemplateRepo
            .Setup(r => r.SingleOrDefaultAsync(It.IsAny<Expression<Func<EmailTemplate, bool>>>()))
            .ReturnsAsync(new EmailTemplate
            {
                TemplateKey = "EmailVerification",
                Subject = "Verify Your SpeedClaim Account",
                BodyHtml = "<p>Click <a href=\"{{verifyUrl}}\">here</a> to verify. &copy; {{year}}</p>",
                IsActive = true
            });
        _mockUnitOfWork.Setup(u => u.EmailTemplates).Returns(mockTemplateRepo.Object);

        await _emailService.SendEmailVerificationAsync("recipient@test.com", "token123");

        _mockSmtpClient.Verify(c => c.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<MailKit.ITransferProgress>()), Times.Once);
    }

    [Test]
    public async Task SendPasswordResetAsync_CallsSendEmail()
    {
        var mockTemplateRepo = new Mock<IRepository<EmailTemplate>>();
        mockTemplateRepo
            .Setup(r => r.SingleOrDefaultAsync(It.IsAny<Expression<Func<EmailTemplate, bool>>>()))
            .ReturnsAsync(new EmailTemplate
            {
                TemplateKey = "PasswordReset",
                Subject = "Reset Your SpeedClaim Password",
                BodyHtml = "<p>Click <a href=\"{{resetUrl}}\">here</a> to reset. &copy; {{year}}</p>",
                IsActive = true
            });
        _mockUnitOfWork.Setup(u => u.EmailTemplates).Returns(mockTemplateRepo.Object);

        await _emailService.SendPasswordResetAsync("recipient@test.com", "token123");

        _mockSmtpClient.Verify(c => c.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<MailKit.ITransferProgress>()), Times.Once);
    }

    [Test]
    public void SendEmailVerificationAsync_MissingTemplate_Throws()
    {
        var mockTemplateRepo = new Mock<IRepository<EmailTemplate>>();
        mockTemplateRepo
            .Setup(r => r.SingleOrDefaultAsync(It.IsAny<Expression<Func<EmailTemplate, bool>>>()))
            .ReturnsAsync((EmailTemplate?)null);
        _mockUnitOfWork.Setup(u => u.EmailTemplates).Returns(mockTemplateRepo.Object);

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _emailService.SendEmailVerificationAsync("recipient@test.com", "token123"));
    }
}
