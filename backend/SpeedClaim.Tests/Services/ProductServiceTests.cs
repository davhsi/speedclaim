using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using AutoMapper;
using SpeedClaim.Api.Context;
using SpeedClaim.Api.Dtos.Catalog;
using SpeedClaim.Api.Mappings;
using SpeedClaim.Api.Models;
using SpeedClaim.Api.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpeedClaim.Tests.Services;

[TestFixture]
public class ProductServiceTests
{
    private SpeedClaimDbContext _context;
    private IMapper _mapper;
    private ProductService _productService;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<SpeedClaimDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SpeedClaimDbContext(options);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(config => config.AddProfile<MappingProfile>());
        var provider = services.BuildServiceProvider();
        _mapper = provider.GetRequiredService<IMapper>();

        _productService = new ProductService(_context, _mapper);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Test]
    public async Task GetAllProductsAsync_ReturnsAllProducts()
    {
        // Arrange
        _context.InsuranceProducts.AddRange(
            new InsuranceProduct { Code = "P1", Name = "Product 1", Domain = "HEALTH", Description = "Desc", IsActive = true },
            new InsuranceProduct { Code = "P2", Name = "Product 2", Domain = "LIFE", Description = "Desc", IsActive = true }
        );
        _context.SaveChanges();

        // Act
        var result = await _productService.GetAllProductsAsync();

        // Assert
        Assert.That(result.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task GetProductsByDomainAsync_ReturnsFilteredProducts()
    {
        // Arrange
        _context.InsuranceProducts.AddRange(
            new InsuranceProduct { Code = "P1", Name = "Product 1", Domain = "HEALTH", Description = "Desc", IsActive = true },
            new InsuranceProduct { Code = "P2", Name = "Product 2", Domain = "HEALTH", Description = "Desc", IsActive = true },
            new InsuranceProduct { Code = "P3", Name = "Product 3", Domain = "VEHICLE", Description = "Desc", IsActive = true }
        );
        _context.SaveChanges();

        // Act
        var result = await _productService.GetProductsByDomainAsync("health");

        // Assert
        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result.All(p => p.Domain == "HEALTH"), Is.True);
    }

    [Test]
    public async Task CreateProductAsync_ValidRequest_CreatesProduct()
    {
        // Arrange
        var request = new CreateProductRequest(
            "AUTO-1",
            "Basic Auto",
            "VEHICLE",
            "Basic auto coverage",
            50000m
        );

        // Act
        var result = await _productService.CreateProductAsync(request);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Code, Is.EqualTo("AUTO-1"));
        
        var dbProduct = await _context.InsuranceProducts.FirstOrDefaultAsync(p => p.Code == "AUTO-1");
        Assert.That(dbProduct, Is.Not.Null);
    }

    [Test]
    public void CreateProductAsync_DuplicateCodeAndDomain_ThrowsInvalidOperationException()
    {
        // Arrange
        _context.InsuranceProducts.Add(new InsuranceProduct { Code = "LIFE-1", Name = "Existing", Domain = "LIFE", Description = "Desc" });
        _context.SaveChanges();

        var request = new CreateProductRequest(
            "LIFE-1",
            "Duplicate",
            "LIFE",
            "",
            null
        );

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await _productService.CreateProductAsync(request));
        Assert.That(ex.Message, Does.Contain("already exists"));
    }

    [Test]
    public async Task UpdateProductAsync_ExistingId_UpdatesProduct()
    {
        // Arrange
        var id = Guid.NewGuid();
        _context.InsuranceProducts.Add(new InsuranceProduct { Id = id, Code = "L1", Name = "Old Name", Domain = "LIFE", Description = "Desc" });
        _context.SaveChanges();

        var request = new UpdateProductRequest(
            "New Name",
            "New Desc",
            1000m,
            false
        );

        // Act
        var result = await _productService.UpdateProductAsync(id, request);

        // Assert
        Assert.That(result.Name, Is.EqualTo("New Name"));
        Assert.That(result.IsActive, Is.False);
    }

    [Test]
    public void UpdateProductAsync_NonExistingId_ThrowsKeyNotFoundException()
    {
        // Arrange
        var request = new UpdateProductRequest("New", "", null, true);

        // Act & Assert
        Assert.ThrowsAsync<KeyNotFoundException>(async () => await _productService.UpdateProductAsync(Guid.NewGuid(), request));
    }

    [Test]
    public async Task DeleteProductAsync_ExistingId_DeactivatesProduct()
    {
        // Arrange
        var id = Guid.NewGuid();
        _context.InsuranceProducts.Add(new InsuranceProduct { Id = id, Code = "L1", Name = "Name", Domain = "LIFE", Description = "Desc", IsActive = true });
        _context.SaveChanges();

        // Act
        await _productService.DeleteProductAsync(id);

        // Assert
        var product = await _context.InsuranceProducts.FindAsync(id);
        Assert.That(product!.IsActive, Is.False);
    }
}
