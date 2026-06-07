using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using AutoMapper;
using Moq;
using SpeedClaim.Api.Dtos.Catalog;
using SpeedClaim.Api.Mappings;
using SpeedClaim.Api.Models;
using SpeedClaim.Api.Services;
using SpeedClaim.Api.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace SpeedClaim.Tests.Services;

[TestFixture]
public class ProductServiceTests
{
    private Mock<IUnitOfWork> _mockUnitOfWork;
    private Mock<IRepository<InsuranceProduct>> _mockProductRepo;
    private IMapper _mapper;
    private ProductService _productService;

    [SetUp]
    public void Setup()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockProductRepo = new Mock<IRepository<InsuranceProduct>>();

        _mockUnitOfWork.Setup(u => u.Products).Returns(_mockProductRepo.Object);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(config => config.AddProfile<MappingProfile>());
        var provider = services.BuildServiceProvider();
        _mapper = provider.GetRequiredService<IMapper>();

        _productService = new ProductService(_mockUnitOfWork.Object, _mapper);
    }

    [Test]
    public async Task GetAllProductsAsync_ReturnsAllProducts()
    {
        var products = new List<InsuranceProduct>
        {
            new InsuranceProduct { Code = "P1", Name = "Product 1", Domain = "HEALTH", Description = "Desc", IsActive = true },
            new InsuranceProduct { Code = "P2", Name = "Product 2", Domain = "LIFE", Description = "Desc", IsActive = true }
        };
        _mockProductRepo.Setup(r => r.GetPagedAsync(1, 10, null, null)).ReturnsAsync((products, products.Count));

        var result = await _productService.GetAllProductsAsync(1, 10);

        Assert.That(result.Data.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task GetProductsByDomainAsync_ReturnsFilteredProducts()
    {
        var products = new List<InsuranceProduct>
        {
            new InsuranceProduct { Code = "P1", Name = "Product 1", Domain = "HEALTH", Description = "Desc", IsActive = true },
            new InsuranceProduct { Code = "P2", Name = "Product 2", Domain = "HEALTH", Description = "Desc", IsActive = true }
        };
        
        _mockProductRepo.Setup(r => r.GetPagedAsync(1, 10, It.IsAny<Expression<Func<InsuranceProduct, bool>>>(), null)).ReturnsAsync((products, products.Count));

        var result = await _productService.GetProductsByDomainAsync("health", 1, 10);

        Assert.That(result.Data.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task CreateProductAsync_ValidRequest_CreatesProduct()
    {
        var request = new CreateProductRequest("AUTO-1", "Basic Auto", "VEHICLE", "Basic auto coverage", 50000m);
        _mockProductRepo.Setup(r => r.SingleOrDefaultAsync(It.IsAny<Expression<Func<InsuranceProduct, bool>>>())).ReturnsAsync((InsuranceProduct?)null);

        var result = await _productService.CreateProductAsync(request);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Code, Is.EqualTo("AUTO-1"));
        _mockProductRepo.Verify(r => r.AddAsync(It.IsAny<InsuranceProduct>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public void CreateProductAsync_DuplicateCodeAndDomain_ThrowsInvalidOperationException()
    {
        var request = new CreateProductRequest("LIFE-1", "Duplicate", "LIFE", "", null);
        var existingProduct = new InsuranceProduct { Code = "LIFE-1", Domain = "LIFE" };
        _mockProductRepo.Setup(r => r.SingleOrDefaultAsync(It.IsAny<Expression<Func<InsuranceProduct, bool>>>())).ReturnsAsync(existingProduct);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await _productService.CreateProductAsync(request));
        Assert.That(ex.Message, Does.Contain("already exists"));
    }

    [Test]
    public async Task UpdateProductAsync_ExistingId_UpdatesProduct()
    {
        var id = Guid.NewGuid();
        var existingProduct = new InsuranceProduct { Id = id, Code = "L1", Name = "Old Name", Domain = "LIFE", Description = "Desc", IsActive = true };
        _mockProductRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existingProduct);

        var request = new UpdateProductRequest("New Name", "New Desc", 1000m, false);

        var result = await _productService.UpdateProductAsync(id, request);

        Assert.That(result.Name, Is.EqualTo("New Name"));
        Assert.That(result.IsActive, Is.False);
        _mockProductRepo.Verify(r => r.Update(existingProduct), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public void UpdateProductAsync_NonExistingId_ThrowsKeyNotFoundException()
    {
        var request = new UpdateProductRequest("New", "", null, true);
        _mockProductRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((InsuranceProduct?)null);

        Assert.ThrowsAsync<KeyNotFoundException>(async () => await _productService.UpdateProductAsync(Guid.NewGuid(), request));
    }

    [Test]
    public async Task DeleteProductAsync_ExistingId_DeactivatesProduct()
    {
        var id = Guid.NewGuid();
        var existingProduct = new InsuranceProduct { Id = id, Code = "L1", Name = "Name", Domain = "LIFE", Description = "Desc", IsActive = true };
        _mockProductRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existingProduct);

        await _productService.DeleteProductAsync(id);

        Assert.That(existingProduct.IsActive, Is.False);
        _mockProductRepo.Verify(r => r.Update(existingProduct), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public void DeleteProductAsync_NonExistingId_ThrowsKeyNotFoundException()
    {
        _mockProductRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((InsuranceProduct?)null);
        Assert.ThrowsAsync<KeyNotFoundException>(async () => await _productService.DeleteProductAsync(Guid.NewGuid()));
    }

    [Test]
    public async Task GetProductByIdAsync_ExistingId_ReturnsProduct()
    {
        var id = Guid.NewGuid();
        var existingProduct = new InsuranceProduct { Id = id, Code = "L1", Name = "Name", Domain = "LIFE" };
        _mockProductRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existingProduct);

        var result = await _productService.GetProductByIdAsync(id);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Code, Is.EqualTo("L1"));
    }

    [Test]
    public void GetProductByIdAsync_NonExistingId_ThrowsKeyNotFoundException()
    {
        _mockProductRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((InsuranceProduct?)null);
        Assert.ThrowsAsync<KeyNotFoundException>(async () => await _productService.GetProductByIdAsync(Guid.NewGuid()));
    }
}
