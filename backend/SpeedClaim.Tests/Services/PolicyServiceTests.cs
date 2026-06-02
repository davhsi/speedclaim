using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using AutoMapper;
using SpeedClaim.Api.Context;
using SpeedClaim.Api.Dtos.Policies;
using SpeedClaim.Api.Mappings;
using SpeedClaim.Api.Models;
using SpeedClaim.Api.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpeedClaim.Tests.Services;

[TestFixture]
public class PolicyServiceTests
{
    private SpeedClaimDbContext _context;
    private IMapper _mapper;
    private PolicyService _policyService;

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

        _policyService = new PolicyService(_context, _mapper);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Test]
    public async Task IssuePolicyAsync_ValidHealthRequest_IssuesPolicyAndReturnsDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        _context.InsuranceProducts.Add(new InsuranceProduct
        {
            Id = productId,
            Code = "H1",
            Name = "Health Plan",
            Domain = "HEALTH",
            Description = "Desc",
            IsActive = true
        });
        _context.SaveChanges();

        var request = new CreatePolicyRequest(
            userId,
            productId,
            1000m,
            50000m,
            "YEARLY",
            DateTime.UtcNow,
            DateTime.UtcNow.AddYears(1),
            "HEALTH",
            new PolicyHealthDetailDto(true, 500m, "PPO"),
            null,
            null
        );

        // Act
        var result = await _policyService.IssuePolicyAsync(request);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.PolicyNumber, Is.Not.Null.And.Not.Empty);
        Assert.That(result.Domain, Is.EqualTo("HEALTH"));
        Assert.That(result.HealthDetail, Is.Not.Null);
        Assert.That(result.HealthDetail!.NetworkType, Is.EqualTo("PPO"));
        
        var savedPolicy = await _context.Policies.Include(p => p.HealthDetail).FirstOrDefaultAsync(p => p.Id == result.Id);
        Assert.That(savedPolicy, Is.Not.Null);
        Assert.That(savedPolicy!.HealthDetail, Is.Not.Null);
    }

    [Test]
    public void IssuePolicyAsync_InvalidProduct_ThrowsArgumentException()
    {
        // Arrange
        var request = new CreatePolicyRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            0,
            0,
            "YEARLY",
            DateTime.UtcNow,
            DateTime.UtcNow,
            "HEALTH",
            null, null, null
        );

        // Act & Assert
        var ex = Assert.ThrowsAsync<ArgumentException>(async () => await _policyService.IssuePolicyAsync(request));
        Assert.That(ex.Message, Is.EqualTo("Invalid product ID."));
    }

    [Test]
    public void IssuePolicyAsync_MismatchedDomain_ThrowsArgumentException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _context.InsuranceProducts.Add(new InsuranceProduct
        {
            Id = productId,
            Code = "V1",
            Name = "Vehicle Plan",
            Domain = "VEHICLE",
            Description = "Desc"
        });
        _context.SaveChanges();

        var request = new CreatePolicyRequest(
            Guid.NewGuid(),
            productId,
            0,
            0,
            "YEARLY",
            DateTime.UtcNow,
            DateTime.UtcNow,
            "HEALTH", // mismatched
            null, null, null
        );

        // Act & Assert
        var ex = Assert.ThrowsAsync<ArgumentException>(async () => await _policyService.IssuePolicyAsync(request));
        Assert.That(ex.Message, Is.EqualTo("Product domain does not match request domain."));
    }

    [Test]
    public async Task GetPolicyByIdAsync_ExistingId_ReturnsPolicy()
    {
        // Arrange
        var policyId = Guid.NewGuid();
        _context.Policies.Add(new Policy
        {
            Id = policyId,
            PolicyNumber = "POL-123",
            Domain = "LIFE",
            Status = "ACTIVE",
            PaymentFrequency = "YEARLY",
            Currency = "INR",
            LifeDetail = new PolicyLifeDetail { NomineeName = "Jane Doe", NomineeRelation = "Spouse", NomineePhone = "1234567890" }
        });
        _context.SaveChanges();

        // Act
        var result = await _policyService.GetPolicyByIdAsync(policyId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.PolicyNumber, Is.EqualTo("POL-123"));
        Assert.That(result.LifeDetail, Is.Not.Null);
        Assert.That(result.LifeDetail!.NomineeName, Is.EqualTo("Jane Doe"));
    }

    [Test]
    public void GetPolicyByIdAsync_NonExistingId_ThrowsKeyNotFoundException()
    {
        // Act & Assert
        Assert.ThrowsAsync<KeyNotFoundException>(async () => await _policyService.GetPolicyByIdAsync(Guid.NewGuid()));
    }

    [Test]
    public async Task GetPoliciesByUserAsync_ReturnsUserPolicies()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _context.Policies.AddRange(
            new Policy { Id = Guid.NewGuid(), UserId = userId, PolicyNumber = "POL-1", Domain = "HEALTH", Status = "ACTIVE", Currency = "INR", PaymentFrequency = "MONTHLY" },
            new Policy { Id = Guid.NewGuid(), UserId = userId, PolicyNumber = "POL-2", Domain = "VEHICLE", Status = "ACTIVE", Currency = "INR", PaymentFrequency = "YEARLY" },
            new Policy { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), PolicyNumber = "POL-3", Domain = "LIFE", Status = "ACTIVE", Currency = "INR", PaymentFrequency = "YEARLY" } // other user
        );
        _context.SaveChanges();

        // Act
        var result = await _policyService.GetPoliciesByUserAsync(userId);

        // Assert
        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result.All(p => p.PolicyNumber == "POL-1" || p.PolicyNumber == "POL-2"), Is.True);
    }
}
