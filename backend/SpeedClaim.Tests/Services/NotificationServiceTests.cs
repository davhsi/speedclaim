using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;
using SpeedClaim.Api.Services;

namespace SpeedClaim.Tests.Services;

[TestFixture]
public class NotificationServiceTests
{
    private Mock<IUnitOfWork> _mockUnitOfWork = null!;
    private Mock<IRepository<Notification>> _mockNotificationRepo = null!;
    private NotificationService _notificationService = null!;

    [SetUp]
    public void Setup()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockNotificationRepo = new Mock<IRepository<Notification>>();
        _mockUnitOfWork.Setup(u => u.Notifications).Returns(_mockNotificationRepo.Object);
        _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);
        _notificationService = new NotificationService(_mockUnitOfWork.Object);
    }

    [Test]
    public async Task CreateAsync_WritesNotificationToDb()
    {
        var userId = Guid.NewGuid();

        await _notificationService.CreateAsync(userId, "Policy Activated", "Your policy is now active.", "policy");

        _mockNotificationRepo.Verify(r => r.AddAsync(It.Is<Notification>(n =>
            n.UserId == userId &&
            n.Title == "Policy Activated" &&
            n.Message == "Your policy is now active." &&
            n.Type == "policy" &&
            !n.IsRead)), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public async Task GetForUserAsync_ReturnsOrderedNotifications()
    {
        var userId = Guid.NewGuid();
        var older = new Notification { Id = Guid.NewGuid(), UserId = userId, Title = "Old", CreatedAt = DateTimeOffset.UtcNow.AddHours(-2) };
        var newer = new Notification { Id = Guid.NewGuid(), UserId = userId, Title = "New", CreatedAt = DateTimeOffset.UtcNow };

        _mockNotificationRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Notification, bool>>>()))
            .ReturnsAsync(new List<Notification> { older, newer });

        var result = (await _notificationService.GetForUserAsync(userId)).ToList();

        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result[0].Title, Is.EqualTo("New"));
        Assert.That(result[1].Title, Is.EqualTo("Old"));
    }

    [Test]
    public async Task MarkAsReadAsync_SetsIsReadTrue()
    {
        var userId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();
        var notification = new Notification { Id = notificationId, UserId = userId, IsRead = false };

        _mockNotificationRepo.Setup(r => r.GetByIdAsync(notificationId)).ReturnsAsync(notification);

        await _notificationService.MarkAsReadAsync(notificationId, userId);

        Assert.That(notification.IsRead, Is.True);
        _mockNotificationRepo.Verify(r => r.Update(notification), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public void MarkAsReadAsync_WrongUser_ThrowsKeyNotFound()
    {
        var notificationId = Guid.NewGuid();
        var notification = new Notification { Id = notificationId, UserId = Guid.NewGuid(), IsRead = false };

        _mockNotificationRepo.Setup(r => r.GetByIdAsync(notificationId)).ReturnsAsync(notification);

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.NotFoundException>(() =>
            _notificationService.MarkAsReadAsync(notificationId, Guid.NewGuid()));
    }

    [Test]
    public async Task MarkAllAsReadAsync_SetsAllUnreadToRead()
    {
        var userId = Guid.NewGuid();
        var n1 = new Notification { Id = Guid.NewGuid(), UserId = userId, IsRead = false };
        var n2 = new Notification { Id = Guid.NewGuid(), UserId = userId, IsRead = false };

        _mockNotificationRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Notification, bool>>>()))
            .ReturnsAsync(new List<Notification> { n1, n2 });

        await _notificationService.MarkAllAsReadAsync(userId);

        Assert.That(n1.IsRead, Is.True);
        Assert.That(n2.IsRead, Is.True);
        _mockNotificationRepo.Verify(r => r.Update(It.IsAny<Notification>()), Times.Exactly(2));
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }
}
