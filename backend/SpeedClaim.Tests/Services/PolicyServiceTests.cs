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
}
