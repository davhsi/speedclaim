using System;
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
    private Mock<IConfiguration> _mockConfiguration;
    private Mock<ILogger<EmailService>> _mockLogger;
    private Mock<IUnitOfWork> _mockUnitOfWork;
    private EmailService _emailService;

    [SetUp]
    public void Setup()
    {
        _mockSmtpClientFactory = new Mock<ISmtpClientFactory>();
        _mockSmtpClient = new Mock<ISmtpClient>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<EmailService>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>() { DefaultValue = DefaultValue.Mock };

        _mockSmtpClientFactory.Setup(f => f.CreateClient()).Returns(_mockSmtpClient.Object);
        
        _mockConfiguration.Setup(c => c["EmailSettings:FromAddress"]).Returns("test@test.com");
        _mockConfiguration.Setup(c => c["EmailSettings:SmtpHost"]).Returns("smtp.test.com");
        _mockConfiguration.Setup(c => c["EmailSettings:SmtpPort"]).Returns("587");
        _mockConfiguration.Setup(c => c["EmailSettings:SmtpUser"]).Returns("user");
        _mockConfiguration.Setup(c => c["EmailSettings:SmtpPass"]).Returns("pass");

        _emailService = new EmailService(
            _mockSmtpClientFactory.Object,
            _mockConfiguration.Object,
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
        _mockSmtpClient.Verify(c => c.AuthenticateAsync("user", "pass", It.IsAny<CancellationToken>()), Times.Once);
        _mockSmtpClient.Verify(c => c.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<MailKit.ITransferProgress>()), Times.Once);
        _mockSmtpClient.Verify(c => c.DisconnectAsync(true, It.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.Verify(u => u.EmailLogs.AddAsync(It.Is<EmailLog>(l => l.RecipientEmail == "recipient@test.com" && l.Status == "Sent")), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
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
        await _emailService.SendEmailVerificationAsync("recipient@test.com", "token123");

        _mockSmtpClient.Verify(c => c.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<MailKit.ITransferProgress>()), Times.Once);
    }

    [Test]
    public async Task SendPasswordResetAsync_CallsSendEmail()
    {
        await _emailService.SendPasswordResetAsync("recipient@test.com", "token123");

        _mockSmtpClient.Verify(c => c.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<MailKit.ITransferProgress>()), Times.Once);
    }
}
