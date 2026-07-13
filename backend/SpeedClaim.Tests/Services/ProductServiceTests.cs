using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SpeedClaim.Api.Dtos.Catalog;
using SpeedClaim.Api.Exceptions;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;
using SpeedClaim.Api.Models.Enums;
using SpeedClaim.Api.Services;

namespace SpeedClaim.Tests.Services;

[TestFixture]
public class ProductServiceTests
{
    private Mock<IUnitOfWork> _mockUnitOfWork = null!;
    private Mock<IRepository<InsuranceProduct>> _mockProductRepo = null!;
    private Mock<IRepository<PremiumRateTable>> _mockRateRepo = null!;
    private Mock<IRepository<DocumentRequirement>> _mockDocReqRepo = null!;
    private Mock<IRepository<Proposal>> _mockProposalRepo = null!;
    private Mock<IPolicyRepository> _mockPolicyRepo = null!;
    private ProductService _productService = null!;

    [SetUp]
    public void Setup()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockProductRepo = new Mock<IRepository<InsuranceProduct>>();
        _mockRateRepo = new Mock<IRepository<PremiumRateTable>>();
        _mockDocReqRepo = new Mock<IRepository<DocumentRequirement>>();
        _mockProposalRepo = new Mock<IRepository<Proposal>>();
        _mockPolicyRepo = new Mock<IPolicyRepository>();

        _mockUnitOfWork.Setup(u => u.InsuranceProducts).Returns(_mockProductRepo.Object);
        _mockUnitOfWork.Setup(u => u.PremiumRateTables).Returns(_mockRateRepo.Object);
        _mockUnitOfWork.Setup(u => u.DocumentRequirements).Returns(_mockDocReqRepo.Object);
        _mockUnitOfWork.Setup(u => u.Proposals).Returns(_mockProposalRepo.Object);
        _mockUnitOfWork.Setup(u => u.Policies).Returns(_mockPolicyRepo.Object);
        _mockUnitOfWork.Setup(u => u.AuditLogs).Returns(new Mock<IRepository<AuditLog>>().Object);
        _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

        _mockProductRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<InsuranceProduct, bool>>>()))
            .ReturnsAsync(new List<InsuranceProduct>());
        _mockProposalRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Proposal, bool>>>()))
            .ReturnsAsync(new List<Proposal>());
        _mockPolicyRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Policy, bool>>>()))
            .ReturnsAsync(new List<Policy>());

        _productService = new ProductService(_mockUnitOfWork.Object);
    }

    [Test]
    public async Task GetAvailableProductsAsync_ReturnsOnlyActiveProductsAvailableForSale()
    {
        var products = new List<InsuranceProduct>
        {
            new InsuranceProduct { Id = Guid.NewGuid(), ProductName = "P1", IsActive = true, IsAvailableForSale = true },
            new InsuranceProduct { Id = Guid.NewGuid(), ProductName = "P2", IsActive = true, IsAvailableForSale = true }
        };
        
        _mockProductRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<InsuranceProduct, bool>>>()))
            .ReturnsAsync(products);

        var result = await _productService.GetAvailableProductsAsync();
        Assert.That(result.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task GetAllProductsAsync_ReturnsActiveAndInactiveProducts()
    {
        var products = new List<InsuranceProduct>
        {
            new InsuranceProduct { Id = Guid.NewGuid(), ProductName = "Active Plan", IsActive = true },
            new InsuranceProduct { Id = Guid.NewGuid(), ProductName = "Retired Plan", IsActive = false }
        };

        _mockProductRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(products);

        var result = (await _productService.GetAllProductsAsync()).ToList();

        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result.Any(p => !p.IsActive), Is.True);
    }

    [Test]
    public async Task CreateProductAsync_ValidRequest_GeneratesUin()
    {
        var request = new CreateProductRequest("Name", "Health", "Desc", 18, 65, 100000, 500000, 1, 10, 30, true, 4);
        var adminId = Guid.NewGuid();

        _mockProductRepo.Setup(r => r.AddAsync(It.IsAny<InsuranceProduct>())).Returns(Task.CompletedTask);

        var result = await _productService.CreateProductAsync(request, adminId.ToString());

        Assert.That(result.ProductName, Is.EqualTo("Name"));
        Assert.That(result.Uin, Is.EqualTo($"UIN-HC-{DateTime.UtcNow.Year}-0001"));
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public async Task CreateProductAsync_ExistingUins_GeneratesNextSequence()
    {
        var year = DateTime.UtcNow.Year;
        var request = new CreateProductRequest("Name", "Motor", "Desc", 18, 65, 100000, 500000, 1, 10, 0, false, 1);

        _mockProductRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<InsuranceProduct, bool>>>()))
            .ReturnsAsync(new List<InsuranceProduct>
            {
                new InsuranceProduct { Id = Guid.NewGuid(), Uin = $"UIN-MO-{year}-0002" },
                new InsuranceProduct { Id = Guid.NewGuid(), Uin = $"UIN-MO-{year}-0007" }
            });

        var result = await _productService.CreateProductAsync(request, Guid.NewGuid().ToString());

        Assert.That(result.Uin, Is.EqualTo($"UIN-MO-{year}-0008"));
    }

    [Test]
    public void UpdatePremiumRateTableAsync_ProductNotFound_ThrowsException()
    {
        _mockProductRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((InsuranceProduct?)null);

        var request = new UpdatePremiumRatesRequest(new List<PremiumRateDto>());
        
        var ex = Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.NotFoundException>(() => _productService.UpdatePremiumRateTableAsync(Guid.NewGuid().ToString(), request, Guid.NewGuid().ToString()));
        Assert.That(ex.Message, Is.EqualTo("Product not found"));
    }

    [Test]
    public void GetPremiumRatesAsync_ProductNotFound_ThrowsException()
    {
        _mockProductRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((InsuranceProduct?)null);

        var ex = Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.NotFoundException>(() =>
            _productService.GetPremiumRatesAsync(Guid.NewGuid().ToString()));

        Assert.That(ex.Message, Is.EqualTo("Product not found"));
    }

    [Test]
    public async Task GetPremiumRatesAsync_ReturnsConfiguredRates()
    {
        var productId = Guid.NewGuid();
        var product = new InsuranceProduct { Id = productId };
        var rates = new List<PremiumRateTable>
        {
            new PremiumRateTable { ProductId = productId, AgeMin = 31, AgeMax = 45, SumAssuredMin = 100000, SumAssuredMax = 500000, AnnualPremium = 8000 },
            new PremiumRateTable { ProductId = productId, AgeMin = 18, AgeMax = 30, SumAssuredMin = 100000, SumAssuredMax = 500000, AnnualPremium = 5000 }
        };

        _mockProductRepo.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _mockRateRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<PremiumRateTable, bool>>>()))
            .ReturnsAsync(rates);

        var result = (await _productService.GetPremiumRatesAsync(productId.ToString())).ToList();

        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result[0].AgeMin, Is.EqualTo(18));
        Assert.That(result[0].AnnualPremium, Is.EqualTo(5000));
    }

    [Test]
    public async Task UpdatePremiumRateTableAsync_ValidRequest_RemovesOldAndAddsNew()
    {
        var productId = Guid.NewGuid();
        var product = new InsuranceProduct { Id = productId };
        var oldRates = new List<PremiumRateTable> { new PremiumRateTable { Id = Guid.NewGuid() } };
        
        _mockProductRepo.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _mockRateRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<PremiumRateTable, bool>>>())).ReturnsAsync(oldRates);

        var request = new UpdatePremiumRatesRequest(new List<PremiumRateDto>
        {
            new PremiumRateDto(18, 25, 100000, 200000, 5000)
        });

        await _productService.UpdatePremiumRateTableAsync(productId.ToString(), request, Guid.NewGuid().ToString());

        _mockRateRepo.Verify(r => r.Delete(It.IsAny<PremiumRateTable>()), Times.Once);
        _mockRateRepo.Verify(r => r.AddAsync(It.IsAny<PremiumRateTable>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public void UpdatePremiumRateTableAsync_MotorOverlappingSumBands_ThrowsValidationException()
    {
        var productId = Guid.NewGuid();
        var product = new InsuranceProduct { Id = productId, Domain = "Motor" };
        _mockProductRepo.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);

        var request = new UpdatePremiumRatesRequest(new List<PremiumRateDto>
        {
            new PremiumRateDto(0, 150, 30000, 50000, 1200),
            new PremiumRateDto(0, 150, 50000, 75000, 1800)
        });

        var ex = Assert.ThrowsAsync<ValidationException>(() =>
            _productService.UpdatePremiumRateTableAsync(productId.ToString(), request, Guid.NewGuid().ToString()));

        Assert.That(ex!.Message, Is.EqualTo("Premium rate bands cannot overlap."));
        _mockRateRepo.Verify(r => r.AddAsync(It.IsAny<PremiumRateTable>()), Times.Never);
    }

    [Test]
    public async Task UpdatePremiumRateTableAsync_NonOverlappingAdjacentMotorBands_Saves()
    {
        var productId = Guid.NewGuid();
        var product = new InsuranceProduct { Id = productId, Domain = "Motor" };
        _mockProductRepo.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _mockRateRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<PremiumRateTable, bool>>>()))
            .ReturnsAsync(new List<PremiumRateTable>());

        var request = new UpdatePremiumRatesRequest(new List<PremiumRateDto>
        {
            new PremiumRateDto(0, 150, 30000, 50000, 1200),
            new PremiumRateDto(0, 150, 50001, 75000, 1800)
        });

        await _productService.UpdatePremiumRateTableAsync(productId.ToString(), request, Guid.NewGuid().ToString());

        _mockRateRepo.Verify(r => r.AddAsync(It.IsAny<PremiumRateTable>()), Times.Exactly(2));
    }

    [Test]
    public async Task ConfigureDocumentRequirementsAsync_AddsRequirements()
    {
        var productId = Guid.NewGuid();
        var product = new InsuranceProduct { Id = productId };
        _mockProductRepo.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _mockDocReqRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<DocumentRequirement, bool>>>()))
                       .ReturnsAsync(new List<DocumentRequirement>());

        var request = new UpdateDocumentRequirementsRequest(new List<DocumentRequirementDto>
        {
            new DocumentRequirementDto(EntityType.Kyc, "health", "ID_PROOF", "ID Proof", "Provide ID", true, true)
        });

        await _productService.ConfigureDocumentRequirementsAsync(productId.ToString(), request, Guid.NewGuid().ToString());

        _mockDocReqRepo.Verify(r => r.AddAsync(It.IsAny<DocumentRequirement>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public async Task ConfigureDocumentRequirementsAsync_WithExistingRequirements_DeletesOldOnes()
    {
        var productId = Guid.NewGuid();
        var product = new InsuranceProduct { Id = productId };
        var existing = new List<DocumentRequirement>
        {
            new DocumentRequirement { Id = Guid.NewGuid(), ProductId = productId, DocumentKey = "OLD_DOC" }
        };
        _mockProductRepo.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _mockDocReqRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<DocumentRequirement, bool>>>()))
                       .ReturnsAsync(existing);

        var request = new UpdateDocumentRequirementsRequest(new List<DocumentRequirementDto>
        {
            new DocumentRequirementDto(EntityType.Kyc, "health", "NEW_DOC", "New Doc", "Provide new doc", true, false)
        });

        await _productService.ConfigureDocumentRequirementsAsync(productId.ToString(), request, Guid.NewGuid().ToString());

        _mockDocReqRepo.Verify(r => r.Delete(It.IsAny<DocumentRequirement>()), Times.Once);
        _mockDocReqRepo.Verify(r => r.AddAsync(It.IsAny<DocumentRequirement>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public void ToggleProductStatusAsync_ProductNotFound_ThrowsException()
    {
        _mockProductRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((InsuranceProduct?)null);

        var ex = Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.NotFoundException>(() => _productService.ToggleProductStatusAsync(Guid.NewGuid().ToString(), false, Guid.NewGuid().ToString()));
        Assert.That(ex.Message, Is.EqualTo("Product not found"));
    }

    [Test]
    public async Task ToggleProductStatusAsync_ValidRequest_TogglesStatus()
    {
        var productId = Guid.NewGuid();
        var product = new InsuranceProduct { Id = productId, IsActive = true };

        _mockProductRepo.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);

        await _productService.ToggleProductStatusAsync(productId.ToString(), false, Guid.NewGuid().ToString());

        Assert.That(product.IsActive, Is.False);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public void ToggleProductStatusAsync_DeactivateWithActiveProposal_ThrowsConflict()
    {
        var productId = Guid.NewGuid();
        var product = new InsuranceProduct { Id = productId, IsActive = true };
        _mockProductRepo.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _mockProposalRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Proposal, bool>>>()))
            .ReturnsAsync(new List<Proposal> { new() { ProductId = productId, Status = ProposalStatus.Submitted } });

        var ex = Assert.ThrowsAsync<ConflictException>(() =>
            _productService.ToggleProductStatusAsync(productId.ToString(), false, Guid.NewGuid().ToString()));

        Assert.That(ex!.Message, Does.Contain("active proposal"));
        Assert.That(product.IsActive, Is.True);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Never);
    }

    [Test]
    public void ToggleProductStatusAsync_DeactivateWithLivePolicy_ThrowsConflict()
    {
        var productId = Guid.NewGuid();
        var product = new InsuranceProduct { Id = productId, IsActive = true };
        _mockProductRepo.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _mockPolicyRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Policy, bool>>>()))
            .ReturnsAsync(new List<Policy> { new() { ProductId = productId, Status = PolicyStatus.Active } });

        var ex = Assert.ThrowsAsync<ConflictException>(() =>
            _productService.ToggleProductStatusAsync(productId.ToString(), false, Guid.NewGuid().ToString()));

        Assert.That(ex!.Message, Does.Contain("live policy"));
        Assert.That(product.IsActive, Is.True);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Never);
    }

    [Test]
    public async Task ToggleProductSaleAvailabilityAsync_WithLivePolicy_WithdrawsWithoutDeactivating()
    {
        var productId = Guid.NewGuid();
        var product = new InsuranceProduct { Id = productId, IsActive = true, IsAvailableForSale = true };
        _mockProductRepo.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _mockPolicyRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Policy, bool>>>()))
            .ReturnsAsync(new List<Policy> { new() { ProductId = productId, Status = PolicyStatus.Active } });

        await _productService.ToggleProductSaleAvailabilityAsync(productId.ToString(), false, Guid.NewGuid().ToString());

        Assert.That(product.IsActive, Is.True);
        Assert.That(product.IsAvailableForSale, Is.False);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public void ToggleProductSaleAvailabilityAsync_RestoreArchivedProduct_ThrowsConflict()
    {
        var productId = Guid.NewGuid();
        var product = new InsuranceProduct { Id = productId, IsActive = false, IsAvailableForSale = false };
        _mockProductRepo.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);

        var ex = Assert.ThrowsAsync<ConflictException>(() =>
            _productService.ToggleProductSaleAvailabilityAsync(productId.ToString(), true, Guid.NewGuid().ToString()));

        Assert.That(ex!.Message, Does.Contain("Archived products"));
        Assert.That(product.IsAvailableForSale, Is.False);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Never);
    }

    [Test]
    public async Task GetByIdAsync_ValidProduct_ReturnsDto()
    {
        var productId = Guid.NewGuid();
        var product = new InsuranceProduct
        {
            Id = productId, ProductName = "Health Basic", Domain = "Health", Uin = "HLT-001",
            Description = "Basic health coverage", MinAge = 18, MaxAge = 65, IsActive = true
        };
        _mockUnitOfWork.Setup(u => u.InsuranceProducts.GetByIdAsync(productId)).ReturnsAsync(product);

        var result = await _productService.GetByIdAsync(productId.ToString());

        Assert.That(result.Id, Is.EqualTo(productId));
        Assert.That(result.ProductName, Is.EqualTo("Health Basic"));
    }

    [Test]
    public void GetByIdAsync_NotFound_ThrowsKeyNotFound()
    {
        _mockUnitOfWork.Setup(u => u.InsuranceProducts.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((InsuranceProduct?)null);

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.NotFoundException>(() => _productService.GetByIdAsync(Guid.NewGuid().ToString()));
    }

    [Test]
    public async Task GetDocumentRequirementsAsync_ValidProduct_ReturnsRequirements()
    {
        var productId = Guid.NewGuid();
        var product = new InsuranceProduct { Id = productId };
        var requirements = new List<DocumentRequirement>
        {
            new DocumentRequirement
            {
                Id = Guid.NewGuid(), ProductId = productId,
                EntityType = EntityType.Kyc, Domain = "health",
                DocumentKey = "ID_PROOF", Label = "ID Proof",
                Description = "Provide a valid ID", IsMandatory = true, IsActive = true
            }
        };

        _mockProductRepo.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _mockDocReqRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<DocumentRequirement, bool>>>()))
            .ReturnsAsync(requirements);

        var result = await _productService.GetDocumentRequirementsAsync(productId.ToString());

        Assert.That(result.Count(), Is.EqualTo(1));
        Assert.That(result.First().DocumentKey, Is.EqualTo("ID_PROOF"));
        Assert.That(result.First().ProductId, Is.EqualTo(productId));
    }

    [Test]
    public void GetDocumentRequirementsAsync_ProductNotFound_ThrowsNotFoundException()
    {
        _mockProductRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((InsuranceProduct?)null);

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.NotFoundException>(() =>
            _productService.GetDocumentRequirementsAsync(Guid.NewGuid().ToString()));
    }
}
