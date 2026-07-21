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
        var brochures = new Mock<IRepository<ProductBrochure>>();
        brochures.Setup(x => x.FindAsync(It.IsAny<Expression<Func<ProductBrochure, bool>>>())).ReturnsAsync(Array.Empty<ProductBrochure>());
        unitOfWork.SetupGet(x => x.ProductBrochures).Returns(brochures.Object);
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
        var brochures = new Mock<IRepository<ProductBrochure>>();
        brochures.Setup(x => x.FindAsync(It.IsAny<Expression<Func<ProductBrochure, bool>>>())).ReturnsAsync(Array.Empty<ProductBrochure>());
        unitOfWork.SetupGet(x => x.ProductBrochures).Returns(brochures.Object);
        unitOfWork.SetupGet(x => x.AuditLogs).Returns(new Mock<IRepository<AuditLog>>().Object);

        SpeedyWorkspaceRequest? captured = null;
        var speedyClient = new Mock<ISpeedyAssistantClient>();
        var workspaceClient = new Mock<ISpeedyWorkspaceClient>();
        workspaceClient.Setup(x => x.AnswerAsync(It.IsAny<SpeedyWorkspaceRequest>(), It.IsAny<CancellationToken>()))
            .Callback<SpeedyWorkspaceRequest, CancellationToken>((request, _) => captured = request)
            .ReturnsAsync(new SpeedyWorkspaceResponse(Guid.NewGuid(), "Explore our products.", "product_discovery", "low", [], [], [], "Fake", "fake-model"));
        var service = new SpeedyAssistantService(unitOfWork.Object, speedyClient.Object, workspaceClient.Object);

        var response = await service.AnswerWorkspaceAsync(null, "Which plans are available?");

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Account.IsAuthenticated, Is.False);
        Assert.That(response.Intent, Is.EqualTo("product_discovery"));
        unitOfWork.Verify(x => x.CompleteAsync(), Times.Never);
    }

    [Test]
    public async Task AnswerWorkspaceAsync_NewCustomerConversation_DoesNotRemarkTheNewParentAsUpdated()
    {
        var userId = Guid.NewGuid();
        var customer = new Customer { Id = Guid.NewGuid(), UserId = userId };
        var products = new Mock<IRepository<InsuranceProduct>>();
        products.Setup(x => x.FindAsync(It.IsAny<Expression<Func<InsuranceProduct, bool>>>() ))
            .ReturnsAsync(Array.Empty<InsuranceProduct>());
        var customers = new Mock<IRepository<Customer>>();
        customers.Setup(x => x.FirstOrDefaultAsync(It.IsAny<Expression<Func<Customer, bool>>>() ))
            .ReturnsAsync(customer);
        var policies = new Mock<IPolicyRepository>();
        policies.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Policy, bool>>>() ))
            .ReturnsAsync(Array.Empty<Policy>());
        var claims = new Mock<IClaimRepository>();
        claims.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Claim, bool>>>() ))
            .ReturnsAsync(Array.Empty<Claim>());
        var proposals = new Mock<IRepository<Proposal>>();
        proposals.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Proposal, bool>>>() ))
            .ReturnsAsync(Array.Empty<Proposal>());
        var grievances = new Mock<IRepository<Grievance>>();
        grievances.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Grievance, bool>>>() ))
            .ReturnsAsync(Array.Empty<Grievance>());
        var conversations = new Mock<IRepository<SpeedyWorkspaceConversation>>();
        var messages = new Mock<IRepository<SpeedyWorkspaceMessage>>();
        var kycs = new Mock<IRepository<KycRecord>>();
        kycs.Setup(x => x.FirstOrDefaultAsync(It.IsAny<Expression<Func<KycRecord, bool>>>() ))
            .ReturnsAsync(new KycRecord
            {
                UserId = userId, KycStatus = SpeedClaim.Api.Models.Enums.KycStatus.Pending,
                AadhaarDocumentKey = "uploads/kyc/aadhaar.pdf", PanDocumentKey = "uploads/kyc/pan.pdf"
            });
        var auditLogs = new Mock<IRepository<AuditLog>>();
        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(new User { Id = userId, FirstName = "Asha" });

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.SetupGet(x => x.InsuranceProducts).Returns(products.Object);
        var brochures = new Mock<IRepository<ProductBrochure>>();
        brochures.Setup(x => x.FindAsync(It.IsAny<Expression<Func<ProductBrochure, bool>>>())).ReturnsAsync(Array.Empty<ProductBrochure>());
        unitOfWork.SetupGet(x => x.ProductBrochures).Returns(brochures.Object);
        unitOfWork.SetupGet(x => x.Customers).Returns(customers.Object);
        unitOfWork.SetupGet(x => x.Users).Returns(users.Object);
        unitOfWork.SetupGet(x => x.Policies).Returns(policies.Object);
        unitOfWork.SetupGet(x => x.Claims).Returns(claims.Object);
        unitOfWork.SetupGet(x => x.Proposals).Returns(proposals.Object);
        unitOfWork.SetupGet(x => x.Grievances).Returns(grievances.Object);
        unitOfWork.SetupGet(x => x.SpeedyWorkspaceConversations).Returns(conversations.Object);
        unitOfWork.SetupGet(x => x.SpeedyWorkspaceMessages).Returns(messages.Object);
        unitOfWork.SetupGet(x => x.KycRecords).Returns(kycs.Object);
        unitOfWork.SetupGet(x => x.AuditLogs).Returns(auditLogs.Object);
        unitOfWork.Setup(x => x.CompleteAsync()).ReturnsAsync(1);

        var workspaceClient = new Mock<ISpeedyWorkspaceClient>();
        SpeedyWorkspaceRequest? captured = null;
        workspaceClient.Setup(x => x.AnswerAsync(It.IsAny<SpeedyWorkspaceRequest>(), It.IsAny<CancellationToken>()))
            .Callback<SpeedyWorkspaceRequest, CancellationToken>((request, _) => captured = request)
            .ReturnsAsync(new SpeedyWorkspaceResponse(Guid.NewGuid(), "Start with your Aadhaar and PAN.", "kyc", "regulated", [], [], [], "Fake", "fake-model"));
        var service = new SpeedyAssistantService(unitOfWork.Object, new Mock<ISpeedyAssistantClient>().Object, workspaceClient.Object);

        var response = await service.AnswerWorkspaceAsync(userId, "Help me complete KYC");

        Assert.That(response.ConversationId, Is.Not.Null);
        conversations.Verify(x => x.AddAsync(It.IsAny<SpeedyWorkspaceConversation>()), Times.Once);
        conversations.Verify(x => x.Update(It.IsAny<SpeedyWorkspaceConversation>()), Times.Never);
        messages.Verify(x => x.AddRangeAsync(It.Is<IEnumerable<SpeedyWorkspaceMessage>>(added => added.Count() == 2)), Times.Once);
        unitOfWork.Verify(x => x.CompleteAsync(), Times.Once);
        Assert.That(captured!.Account.Kyc, Is.Not.Null);
        Assert.That(captured.Account.Kyc!.Status, Is.EqualTo("Pending"));
        Assert.That(captured.Account.Kyc.AadhaarUploaded && captured.Account.Kyc.PanUploaded, Is.True);
    }
}
