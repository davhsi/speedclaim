using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SpeedClaim.Api.Dtos.SystemManagement;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;
using SpeedClaim.Api.Services;

namespace SpeedClaim.Tests.Services;

[TestFixture]
public class SystemServiceTests
{
    private Mock<IUnitOfWork> _mockUnitOfWork;
    private SystemService _systemService;

    [SetUp]
    public void Setup()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>() { DefaultValue = DefaultValue.Mock };
        _systemService = new SystemService(_mockUnitOfWork.Object);
    }

    [Test]
    public async Task GetSystemConfigsAsync_ReturnsConfigs()
    {
        var configs = new List<SystemConfig>
        {
            new SystemConfig { Id = Guid.NewGuid(), ConfigKey = "Test1", ConfigValue = "Val1" }
        };
        _mockUnitOfWork.Setup(u => u.SystemConfigs.GetAllAsync()).ReturnsAsync(configs);

        var result = await _systemService.GetSystemConfigsAsync();

        Assert.That(result.Count(), Is.EqualTo(1));
        Assert.That(result.First().ConfigKey, Is.EqualTo("Test1"));
    }

    [Test]
    public async Task UpdateSystemConfigAsync_CreatesNewConfigIfNotFound()
    {
        _mockUnitOfWork.Setup(u => u.SystemConfigs.SingleOrDefaultAsync(It.IsAny<Expression<Func<SystemConfig, bool>>>()))
            .ReturnsAsync((SystemConfig)null);

        var request = new UpdateSystemConfigRequest("NewKey", "NewVal");
        await _systemService.UpdateSystemConfigAsync(request, Guid.NewGuid());

        _mockUnitOfWork.Verify(u => u.SystemConfigs.AddAsync(It.Is<SystemConfig>(c => c.ConfigKey == "NewKey" && c.ConfigValue == "NewVal")), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public async Task UpdateSystemConfigAsync_UpdatesExistingConfig()
    {
        var existingConfig = new SystemConfig { Id = Guid.NewGuid(), ConfigKey = "OldKey", ConfigValue = "OldVal" };
        _mockUnitOfWork.Setup(u => u.SystemConfigs.SingleOrDefaultAsync(It.IsAny<Expression<Func<SystemConfig, bool>>>()))
            .ReturnsAsync(existingConfig);

        var request = new UpdateSystemConfigRequest("OldKey", "NewVal");
        await _systemService.UpdateSystemConfigAsync(request, Guid.NewGuid());

        Assert.That(existingConfig.ConfigValue, Is.EqualTo("NewVal"));
        _mockUnitOfWork.Verify(u => u.SystemConfigs.Update(existingConfig), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public async Task ManageEmailTemplatesAsync_CreatesNewTemplateIfNotFound()
    {
        _mockUnitOfWork.Setup(u => u.EmailTemplates.SingleOrDefaultAsync(It.IsAny<Expression<Func<EmailTemplate, bool>>>()))
            .ReturnsAsync((EmailTemplate)null);

        var request = new ManageEmailTemplateRequest("Tpl1", "Sub1", "Body1");
        await _systemService.ManageEmailTemplatesAsync(request, Guid.NewGuid());

        _mockUnitOfWork.Verify(u => u.EmailTemplates.AddAsync(It.Is<EmailTemplate>(t => t.TemplateKey == "Tpl1")), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public async Task GetAuditLogsAsync_ReturnsLogs()
    {
        var actorId = Guid.NewGuid();
        var logs = new List<AuditLog>
        {
            new AuditLog { Id = Guid.NewGuid(), Action = "Test", UserId = actorId },
            new AuditLog { Id = Guid.NewGuid(), Action = "SystemAction", UserId = null }
        };
        _mockUnitOfWork.Setup(u => u.AuditLogs.GetAllAsync()).ReturnsAsync(logs);

        var users = new List<User> { new User { Id = actorId, FirstName = "Davish", LastName = "Official" } };
        _mockUnitOfWork.Setup(u => u.Users.GetAllAsync()).ReturnsAsync(users);

        var result = (await _systemService.GetAuditLogsAsync()).ToList();

        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result[0].Action, Is.EqualTo("Test"));
        Assert.That(result[0].UserName, Is.EqualTo("Davish Official"));
        Assert.That(result[1].UserName, Is.Null); // no UserId -> no name
    }

    [Test]
    public async Task GetNotificationsAndEmailLogsAsync_ReturnsNotifications()
    {
        var notifications = new List<Notification>
        {
            new Notification { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Title = "Test", Message = "Msg", Type = "policy", IsRead = false }
        };
        _mockUnitOfWork.Setup(u => u.Notifications.GetAllAsync()).ReturnsAsync(notifications);

        var result = await _systemService.GetNotificationsAndEmailLogsAsync();

        Assert.That(result.Count(), Is.EqualTo(1));
        Assert.That(result.First().Title, Is.EqualTo("Test"));
    }

    [Test]
    public async Task GetEmailTemplatesAsync_ReturnsAllTemplates()
    {
        var templates = new List<EmailTemplate>
        {
            new EmailTemplate { Id = Guid.NewGuid(), TemplateKey = "Welcome", Subject = "Welcome!", BodyHtml = "<p>Hi</p>", IsActive = true, CreatedAt = DateTimeOffset.UtcNow }
        };
        _mockUnitOfWork.Setup(u => u.EmailTemplates.GetAllAsync()).ReturnsAsync(templates);

        var result = (await _systemService.GetEmailTemplatesAsync()).ToList();

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].TemplateKey, Is.EqualTo("Welcome"));
        Assert.That(result[0].Subject, Is.EqualTo("Welcome!"));
        Assert.That(result[0].IsActive, Is.True);
    }

    [Test]
    public async Task GetEmailTemplatesAsync_NoTemplates_ReturnsEmpty()
    {
        _mockUnitOfWork.Setup(u => u.EmailTemplates.GetAllAsync()).ReturnsAsync(new List<EmailTemplate>());

        var result = (await _systemService.GetEmailTemplatesAsync()).ToList();

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task ManageEmailTemplatesAsync_UpdatesExistingTemplate()
    {
        var existing = new EmailTemplate { Id = Guid.NewGuid(), TemplateKey = "WelcomeEmail", Subject = "Old Subject", BodyHtml = "Old Body" };
        _mockUnitOfWork.Setup(u => u.EmailTemplates.SingleOrDefaultAsync(It.IsAny<Expression<Func<EmailTemplate, bool>>>()))
            .ReturnsAsync(existing);

        var request = new ManageEmailTemplateRequest("WelcomeEmail", "New Subject", "New Body");
        await _systemService.ManageEmailTemplatesAsync(request, Guid.NewGuid());

        Assert.That(existing.Subject, Is.EqualTo("New Subject"));
        Assert.That(existing.BodyHtml, Is.EqualTo("New Body"));
        _mockUnitOfWork.Verify(u => u.EmailTemplates.Update(existing), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }
}
