using System;
using System.Threading;
using System.Threading.Tasks;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using Moq;
using NUnit.Framework;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Services;

namespace SpeedClaim.Tests.Services;

[TestFixture]
public class EmailServiceTests
{
    private Mock<IConfiguration> _configMock;
    private Mock<ILogger<EmailService>> _loggerMock;
    private Mock<ISmtpClientFactory> _smtpFactoryMock;
    private Mock<ISmtpClient> _smtpClientMock;
    private EmailService _emailService;

    [SetUp]
    public void SetUp()
    {
        _configMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<EmailService>>();
        _smtpFactoryMock = new Mock<ISmtpClientFactory>();
        _smtpClientMock = new Mock<ISmtpClient>();

        _smtpFactoryMock.Setup(x => x.CreateClient()).Returns(_smtpClientMock.Object);

        // Setup default configuration
        var smtpSettingsMock = new Mock<IConfigurationSection>();
        smtpSettingsMock.Setup(s => s["Host"]).Returns("smtp.test.com");
        smtpSettingsMock.Setup(s => s["Port"]).Returns("587");
        smtpSettingsMock.Setup(s => s["SenderName"]).Returns("Test Sender");
        smtpSettingsMock.Setup(s => s["SenderEmail"]).Returns("test@example.com");
        smtpSettingsMock.Setup(s => s["AppPassword"]).Returns("valid_password");

        _configMock.Setup(c => c.GetSection("SmtpSettings")).Returns(smtpSettingsMock.Object);

        _emailService = new EmailService(_configMock.Object, _loggerMock.Object, _smtpFactoryMock.Object);
    }

    [Test]
    public async Task SendEmailAsync_WithValidConfig_SendsEmail()
    {
        // Act
        await _emailService.SendEmailAsync("user@example.com", "Test User", "Subject", "<p>Body</p>");

        // Assert
        _smtpClientMock.Verify(x => x.ConnectAsync("smtp.test.com", 587, SecureSocketOptions.StartTls, It.IsAny<CancellationToken>()), Times.Once);
        _smtpClientMock.Verify(x => x.AuthenticateAsync("test@example.com", "valid_password", It.IsAny<CancellationToken>()), Times.Once);
        
        _smtpClientMock.Verify(x => x.SendAsync(
            It.Is<MimeMessage>(m => 
                m.To.Count == 1 && 
                m.To[0].Name == "Test User" && 
                m.Subject == "Subject" && 
                m.HtmlBody != null && m.HtmlBody.Contains("<p>Body</p>")
            ), 
            It.IsAny<CancellationToken>(), 
            It.IsAny<ITransferProgress>()), 
            Times.Once);
            
        _smtpClientMock.Verify(x => x.DisconnectAsync(true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SendEmailAsync_WithEmptyPassword_SkipsSending()
    {
        // Arrange
        var smtpSettingsMock = new Mock<IConfigurationSection>();
        smtpSettingsMock.Setup(s => s["AppPassword"]).Returns("");
        _configMock.Setup(c => c.GetSection("SmtpSettings")).Returns(smtpSettingsMock.Object);

        // Act
        await _emailService.SendEmailAsync("user@example.com", "Test User", "Subject", "<p>Body</p>");

        // Assert
        _smtpClientMock.Verify(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task SendEmailAsync_WithDummyPassword_SkipsSending()
    {
        // Arrange
        var smtpSettingsMock = new Mock<IConfigurationSection>();
        smtpSettingsMock.Setup(s => s["AppPassword"]).Returns("dummy_password");
        _configMock.Setup(c => c.GetSection("SmtpSettings")).Returns(smtpSettingsMock.Object);

        // Act
        await _emailService.SendEmailAsync("user@example.com", "Test User", "Subject", "<p>Body</p>");

        // Assert
        _smtpClientMock.Verify(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task SendEmailAsync_WhenExceptionThrown_LogsError()
    {
        // Arrange
        _smtpClientMock.Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(), It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new Exception("Network error"));

        // Act
        await _emailService.SendEmailAsync("user@example.com", "Test User", "Subject", "<p>Body</p>");

        // Assert
        // We verify the exception was handled, meaning the test shouldn't throw
        _smtpClientMock.Verify(x => x.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task SendEmailAsync_WithMissingConfig_UsesDefaults()
    {
        // Arrange
        var emptyConfigMock = new Mock<IConfigurationSection>();
        emptyConfigMock.Setup(s => s["AppPassword"]).Returns("valid_password");
        // Returning null for Host, Port, SenderName, SenderEmail
        _configMock.Setup(c => c.GetSection("SmtpSettings")).Returns(emptyConfigMock.Object);

        // Act
        await _emailService.SendEmailAsync("user@example.com", "Test User", "Subject", "<p>Body</p>");

        // Assert
        _smtpClientMock.Verify(x => x.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls, It.IsAny<CancellationToken>()), Times.Once);
        _smtpClientMock.Verify(x => x.AuthenticateAsync("noreply@speedclaim.com", "valid_password", It.IsAny<CancellationToken>()), Times.Once);
        
        _smtpClientMock.Verify(x => x.SendAsync(
            It.Is<MimeMessage>(m => 
                m.From.Count == 1 && 
                m.From[0].Name == "SpeedClaim"
            ), 
            It.IsAny<CancellationToken>(), 
            It.IsAny<ITransferProgress>()), 
            Times.Once);
    }
}
