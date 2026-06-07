using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using AutoMapper;
using Moq;
using SpeedClaim.Api.Dtos.Policies;
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
public class PolicyServiceTests
{
    private Mock<IUnitOfWork> _mockUnitOfWork;
    private Mock<IPolicyRepository> _mockPolicyRepo;
    private Mock<IRepository<InsuranceProduct>> _mockProductRepo;
    private Mock<IRepository<Agent>> _mockAgentRepo;
    private IMapper _mapper;
    private PolicyService _policyService;

    [SetUp]
    public void Setup()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockPolicyRepo = new Mock<IPolicyRepository>();
        _mockProductRepo = new Mock<IRepository<InsuranceProduct>>();
        _mockAgentRepo = new Mock<IRepository<Agent>>();

        _mockUnitOfWork.Setup(u => u.Policies).Returns(_mockPolicyRepo.Object);
        _mockUnitOfWork.Setup(u => u.Products).Returns(_mockProductRepo.Object);
        _mockUnitOfWork.Setup(u => u.Agents).Returns(_mockAgentRepo.Object);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(config => config.AddProfile<MappingProfile>());
        var provider = services.BuildServiceProvider();
        _mapper = provider.GetRequiredService<IMapper>();

        _policyService = new PolicyService(_mockUnitOfWork.Object, _mapper);
    }

    [Test]
    public async Task IssuePolicyAsync_ValidRequest_IssuesPendingPolicy()
    {
        var productId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var product = new InsuranceProduct { Id = productId, Domain = "HEALTH" };
        
        _mockProductRepo.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _mockPolicyRepo.Setup(r => r.AddAsync(It.IsAny<Policy>())).Callback<Policy>(p => p.Id = Guid.NewGuid());
        _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);
        _mockPolicyRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new HealthPolicy { Status = "PENDING" });

        var request = new CreatePolicyRequest(
            UserId: userId,
            ProductId: productId,
            PremiumAmount: 5000m,
            CoverageAmount: 1000000m,
            PaymentFrequency: "YEARLY",
            StartDate: DateTime.UtcNow,
            EndDate: DateTime.UtcNow.AddYears(1),
            Domain: "HEALTH",
            HealthDetail: new PolicyHealthDetailDto(true, 5000m, "TPA"),
            VehicleDetail: null,
            LifeDetail: null
        );

        var result = await _policyService.IssuePolicyAsync(request, null);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Status, Is.EqualTo("PENDING"));
        _mockPolicyRepo.Verify(r => r.AddAsync(It.IsAny<Policy>()), Times.Once);
    }

    [Test]
    public void IssuePolicyAsync_InvalidDomain_ThrowsArgumentException()
    {
        var productId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var product = new InsuranceProduct { Id = productId, Domain = "HEALTH" };
        
        _mockProductRepo.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);

        var request = new CreatePolicyRequest(
            UserId: userId,
            ProductId: productId,
            PremiumAmount: 5000m,
            CoverageAmount: 1000000m,
            PaymentFrequency: "YEARLY",
            StartDate: DateTime.UtcNow,
            EndDate: DateTime.UtcNow.AddYears(1),
            Domain: "VEHICLE", // Mismatch
            HealthDetail: null,
            VehicleDetail: null,
            LifeDetail: null
        );

        var ex = Assert.ThrowsAsync<ArgumentException>(async () => await _policyService.IssuePolicyAsync(request, null));
        Assert.That(ex.Message, Does.Contain("domain does not match"));
    }

    [Test]
    public async Task GetPoliciesByUserAsync_ReturnsUserPolicies()
    {
        var userId = Guid.NewGuid();
        var policies = new List<Policy>
        {
            new HealthPolicy { UserId = userId, Status = "ACTIVE", Domain = "HEALTH", PolicyNumber = "H1" },
            new VehiclePolicy { UserId = userId, Status = "PENDING", Domain = "VEHICLE", PolicyNumber = "V1" }
        };

        _mockPolicyRepo.Setup(r => r.GetPagedAsync(1, 10, It.IsAny<Expression<Func<Policy, bool>>>(), It.IsAny<Func<System.Linq.IQueryable<Policy>, System.Linq.IQueryable<Policy>>>())).ReturnsAsync((policies, policies.Count));

        var result = await _policyService.GetPoliciesByUserAsync(userId, 1, 10);

        Assert.That(result.Data.Count(), Is.EqualTo(2));
    }

    [Test]
    public void IssuePolicyAsync_InvalidProduct_ThrowsArgumentException()
    {
        _mockProductRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((InsuranceProduct?)null);

        var request = new CreatePolicyRequest(Guid.NewGuid(), Guid.NewGuid(), 5000m, 1000000m, "YEARLY", DateTime.UtcNow, DateTime.UtcNow.AddYears(1), "HEALTH", null, null, null);

        var ex = Assert.ThrowsAsync<ArgumentException>(async () => await _policyService.IssuePolicyAsync(request, null));
        Assert.That(ex.Message, Does.Contain("Invalid product ID."));
    }

    [Test]
    public void IssuePolicyAsync_UnknownDomain_ThrowsArgumentException()
    {
        var productId = Guid.NewGuid();
        var product = new InsuranceProduct { Id = productId, Domain = "HOME" };
        _mockProductRepo.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);

        var request = new CreatePolicyRequest(Guid.NewGuid(), productId, 5000m, 1000000m, "YEARLY", DateTime.UtcNow, DateTime.UtcNow.AddYears(1), "HOME", null, null, null);

        var ex = Assert.ThrowsAsync<ArgumentException>(async () => await _policyService.IssuePolicyAsync(request, null));
        Assert.That(ex.Message, Does.Contain("Unknown domain"));
    }

    [Test]
    public async Task IssuePolicyAsync_VehicleDomain_MonthlyFrequency_WithAgent_IssuesPolicy()
    {
        var productId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var agentUserId = Guid.NewGuid();
        var agentId = Guid.NewGuid();

        var product = new InsuranceProduct { Id = productId, Domain = "VEHICLE" };
        var agent = new Agent { Id = agentId, UserId = agentUserId };
        
        _mockProductRepo.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _mockAgentRepo.Setup(r => r.SingleOrDefaultAsync(It.IsAny<Expression<Func<Agent, bool>>>())).ReturnsAsync(agent);
        
        Policy capturedPolicy = null;
        _mockPolicyRepo.Setup(r => r.AddAsync(It.IsAny<Policy>())).Callback<Policy>(p => 
        { 
            p.Id = Guid.NewGuid(); 
            capturedPolicy = p; 
        });
        
        _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);
        _mockPolicyRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(() => capturedPolicy);

        var request = new CreatePolicyRequest(
            UserId: userId,
            ProductId: productId,
            PremiumAmount: 12000m,
            CoverageAmount: 500000m,
            PaymentFrequency: "MONTHLY",
            StartDate: DateTime.UtcNow,
            EndDate: DateTime.UtcNow.AddYears(1),
            Domain: "VEHICLE",
            HealthDetail: null,
            VehicleDetail: new PolicyVehicleDetailDto("KA011234", "Toyota", "Camry", 2020, 450000m, true),
            LifeDetail: null
        );

        var result = await _policyService.IssuePolicyAsync(request, agentUserId);

        Assert.That(result, Is.Not.Null);
        Assert.That(capturedPolicy, Is.InstanceOf<VehiclePolicy>());
        Assert.That(capturedPolicy.AgentId, Is.EqualTo(agentId));
        Assert.That(capturedPolicy.PremiumSchedules.Count, Is.EqualTo(12));
    }

    [Test]
    public async Task IssuePolicyAsync_LifeDomain_QuarterlyFrequency_IssuesPolicy()
    {
        var productId = Guid.NewGuid();
        var product = new InsuranceProduct { Id = productId, Domain = "LIFE" };
        _mockProductRepo.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        
        Policy capturedPolicy = null;
        _mockPolicyRepo.Setup(r => r.AddAsync(It.IsAny<Policy>())).Callback<Policy>(p => 
        { 
            p.Id = Guid.NewGuid(); 
            capturedPolicy = p; 
        });
        
        _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);
        _mockPolicyRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(() => capturedPolicy);

        var request = new CreatePolicyRequest(Guid.NewGuid(), productId, 4000m, 1000000m, "QUARTERLY", DateTime.UtcNow, DateTime.UtcNow.AddYears(1), "LIFE", null, null, new PolicyLifeDetailDto("Jane Doe", "Spouse", "123456", false));

        var result = await _policyService.IssuePolicyAsync(request, null);

        Assert.That(result, Is.Not.Null);
        Assert.That(capturedPolicy, Is.InstanceOf<LifePolicy>());
        Assert.That(capturedPolicy.PremiumSchedules.Count, Is.EqualTo(4));
    }

    [Test]
    public void GetPolicyByIdAsync_NotFound_ThrowsKeyNotFoundException()
    {
        _mockPolicyRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Policy?)null);
        Assert.ThrowsAsync<KeyNotFoundException>(async () => await _policyService.GetPolicyByIdAsync(Guid.NewGuid()));
    }

    [Test]
    public async Task IssuePolicyAsync_AnnualFrequency_GeneratesSchedules()
    {
        var product = new InsuranceProduct { Id = Guid.NewGuid(), Code = "PRD1", Domain = "HEALTH" };
        var request = new CreatePolicyRequest(Guid.NewGuid(), product.Id, 1000m, 50000m, "ANNUAL", DateTime.UtcNow, DateTime.UtcNow.AddYears(1), "HEALTH", null, null, null);

        _mockProductRepo.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);
        _mockPolicyRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new HealthPolicy { Status = "PENDING", PaymentFrequency = "ANNUAL" });

        var result = await _policyService.IssuePolicyAsync(request);

        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }
}
