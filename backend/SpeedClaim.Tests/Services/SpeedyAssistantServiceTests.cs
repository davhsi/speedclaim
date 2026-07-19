using System.Linq.Expressions;
using Moq;
using NUnit.Framework;
using SpeedClaim.Api.Dtos.Assistant;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;
using SpeedClaim.Api.Services;

namespace SpeedClaim.Tests.Services;

[TestFixture]
public class SpeedyAssistantServiceTests
{
    [Test]
    public async Task AnswerAsync_Guest_UsesOnlySaleableCatalogWithoutCreatingAnAuditRecord()
    {
        var products = new Mock<IRepository<InsuranceProduct>>();
        products.Setup(x => x.FindAsync(It.IsAny<Expression<Func<InsuranceProduct, bool>>>() ))
            .ReturnsAsync(new[]
            {
                new InsuranceProduct
                {
                    ProductName = "Family Shield", Domain = "HEALTH", Description = "Family medical cover.",
                    MinAge = 18, MaxAge = 65, MinSumAssured = 100000, MaxSumAssured = 1000000,
                    MinTenureYears = 1, MaxTenureYears = 3, WaitingPeriodDays = 30,
                    AllowsFamilyFloater = true, MaxFamilyMembers = 6, IsActive = true, IsAvailableForSale = true
                }
            });
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.SetupGet(x => x.InsuranceProducts).Returns(products.Object);
        unitOfWork.SetupGet(x => x.AuditLogs).Returns(new Mock<IRepository<AuditLog>>().Object);

        SpeedyAssistantRequest? captured = null;
        var client = new Mock<ISpeedyAssistantClient>();
        client.Setup(x => x.AnswerAsync(It.IsAny<SpeedyAssistantRequest>(), It.IsAny<CancellationToken>()))
            .Callback<SpeedyAssistantRequest, CancellationToken>((request, _) => captured = request)
            .ReturnsAsync(new SpeedyAssistantResponse(Guid.NewGuid(), "Family Shield supports a family floater.", "Fake", "fake-model"));
        var service = new SpeedyAssistantService(unitOfWork.Object, client.Object);

        await service.AnswerAsync(null, "Which plans allow family cover?");

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Account.IsAuthenticated, Is.False);
        Assert.That(captured.Account.Policies, Is.Empty);
        Assert.That(captured.Catalog.Products, Has.Count.EqualTo(1));
        Assert.That(captured.Catalog.Products[0].ProductName, Is.EqualTo("Family Shield"));
        unitOfWork.Verify(x => x.CompleteAsync(), Times.Never);
    }

    [Test]
    public async Task AnswerWorkspaceAsync_Guest_UsesTheWorkspaceClientWithoutCreatingAnAuditRecord()
    {
        var products = new Mock<IRepository<InsuranceProduct>>();
        products.Setup(x => x.FindAsync(It.IsAny<Expression<Func<InsuranceProduct, bool>>>() ))
            .ReturnsAsync(Array.Empty<InsuranceProduct>());
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.SetupGet(x => x.InsuranceProducts).Returns(products.Object);
        unitOfWork.SetupGet(x => x.AuditLogs).Returns(new Mock<IRepository<AuditLog>>().Object);

        SpeedyWorkspaceRequest? captured = null;
        var speedyClient = new Mock<ISpeedyAssistantClient>();
        var workspaceClient = new Mock<ISpeedyWorkspaceClient>();
        workspaceClient.Setup(x => x.AnswerAsync(It.IsAny<SpeedyWorkspaceRequest>(), It.IsAny<CancellationToken>()))
            .Callback<SpeedyWorkspaceRequest, CancellationToken>((request, _) => captured = request)
            .ReturnsAsync(new SpeedyWorkspaceResponse(Guid.NewGuid(), "Explore our products.", "product_discovery", "low", [], "Fake", "fake-model"));
        var service = new SpeedyAssistantService(unitOfWork.Object, speedyClient.Object, workspaceClient.Object);

        var response = await service.AnswerWorkspaceAsync(null, "Which plans are available?");

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Account.IsAuthenticated, Is.False);
        Assert.That(response.Intent, Is.EqualTo("product_discovery"));
        unitOfWork.Verify(x => x.CompleteAsync(), Times.Never);
    }
}
