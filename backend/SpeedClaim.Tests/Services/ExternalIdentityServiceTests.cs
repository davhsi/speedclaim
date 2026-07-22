using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SpeedClaim.Api.Exceptions;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;
using SpeedClaim.Api.Models.Enums;
using SpeedClaim.Api.Services;

namespace SpeedClaim.Tests.Services;

[TestFixture]
public class ExternalIdentityServiceTests
{
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Mock<IUserRepository> _users = null!;
    private Mock<IRepository<ExternalIdentity>> _identities = null!;
    private ExternalIdentityService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _unitOfWork = new Mock<IUnitOfWork>();
        _users = new Mock<IUserRepository>();
        _identities = new Mock<IRepository<ExternalIdentity>>();

        _unitOfWork.Setup(unit => unit.Users).Returns(_users.Object);
        _unitOfWork.Setup(unit => unit.ExternalIdentities).Returns(_identities.Object);
        _unitOfWork.Setup(unit => unit.AuditLogs).Returns(Mock.Of<IRepository<AuditLog>>());
        _unitOfWork.Setup(unit => unit.CompleteAsync()).ReturnsAsync(1);
        _service = new ExternalIdentityService(_unitOfWork.Object, Mock.Of<Microsoft.Extensions.Logging.ILogger<ExternalIdentityService>>());
    }

    [Test]
    public async Task LinkAuth0SubjectAsync_CreatesAuditedSubjectMappingForVerifiedCustomer()
    {
        var userId = Guid.NewGuid();
        ExternalIdentity? added = null;
        _users.Setup(users => users.GetByIdAsync(userId)).ReturnsAsync(Customer(userId));
        _identities.Setup(identities => identities.FirstOrDefaultAsync(It.IsAny<Expression<Func<ExternalIdentity, bool>>>() ))
            .ReturnsAsync((ExternalIdentity?)null);
        _identities.Setup(identities => identities.AddAsync(It.IsAny<ExternalIdentity>()))
            .Callback<ExternalIdentity>(identity => added = identity)
            .Returns(Task.CompletedTask);

        await _service.LinkAuth0SubjectAsync(userId, "auth0|customer-123");

        Assert.That(added, Is.Not.Null);
        Assert.That(added!.UserId, Is.EqualTo(userId));
        Assert.That(added.Provider, Is.EqualTo(ExternalIdentityService.Auth0Provider));
        Assert.That(added.Subject, Is.EqualTo("auth0|customer-123"));
        _unitOfWork.Verify(unit => unit.CompleteAsync(), Times.Once);
    }

    [Test]
    public void LinkAuth0SubjectAsync_RejectsSubjectAlreadyLinkedToAnotherCustomer()
    {
        var userId = Guid.NewGuid();
        _users.Setup(users => users.GetByIdAsync(userId)).ReturnsAsync(Customer(userId));
        _identities.Setup(identities => identities.FirstOrDefaultAsync(It.IsAny<Expression<Func<ExternalIdentity, bool>>>() ))
            .ReturnsAsync(new ExternalIdentity { UserId = Guid.NewGuid(), Provider = ExternalIdentityService.Auth0Provider, Subject = "auth0|other" });

        Assert.ThrowsAsync<ConflictException>(() => _service.LinkAuth0SubjectAsync(userId, "auth0|other"));
    }

    private static User Customer(Guid userId) => new()
    {
        Id = userId,
        Role = UserRole.Customer,
        IsActive = true,
        IsEmailVerified = true
    };
}
