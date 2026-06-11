using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using SpeedClaim.Api.Models;
using SpeedClaim.Api.Models.Enums;
using SpeedClaim.Api.Services;

namespace SpeedClaim.Tests.Services;

[TestFixture]
public class JwtServiceTests
{
    private Mock<IConfiguration> _mockConfig = null!;
    private Mock<IConfigurationSection> _mockSection = null!;
    private JwtService _jwtService = null!;

    [SetUp]
    public void Setup()
    {
        _mockConfig = new Mock<IConfiguration>();
        _mockSection = new Mock<IConfigurationSection>();

        _mockSection.Setup(s => s["Secret"]).Returns("SuperSecretKeyThatIsAtLeast32BytesLong!!");
        _mockSection.Setup(s => s["Issuer"]).Returns("SpeedClaimApi");
        _mockSection.Setup(s => s["Audience"]).Returns("SpeedClaimClients");
        _mockSection.Setup(s => s["AccessTokenExpirationMinutes"]).Returns("15");

        _mockConfig.Setup(c => c.GetSection("JwtSettings")).Returns(_mockSection.Object);

        _jwtService = new JwtService(_mockConfig.Object);
    }

    [Test]
    public void GenerateAccessToken_ValidUser_ReturnsValidJwt()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            Role = UserRole.Customer
        };

        var token = _jwtService.GenerateAccessToken(user);

        Assert.That(token, Is.Not.Null.And.Not.Empty);
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        Assert.That(jwt.Issuer, Is.EqualTo("SpeedClaimApi"));
        Assert.That(jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value, Is.EqualTo("test@example.com"));
        Assert.That(jwt.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Customer"), Is.True);
    }

    [Test]
    public void GenerateAccessToken_UserWithKycRecord_IncludesKycStatus()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "kyc@example.com",
            FirstName = "KYC",
            LastName = "User",
            Role = UserRole.Customer,
            KycRecord = new KycRecord { KycStatus = KycStatus.Approved }
        };

        var token = _jwtService.GenerateAccessToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        Assert.That(jwt.Claims.Any(c => c.Type == "kycStatus" && c.Value == "Approved"), Is.True);
    }

    [Test]
    public void GenerateRefreshToken_ReturnsDifferentTokensEachCall()
    {
        var t1 = _jwtService.GenerateRefreshToken();
        var t2 = _jwtService.GenerateRefreshToken();

        Assert.That(t1, Is.Not.Null.And.Not.Empty);
        Assert.That(t2, Is.Not.Null.And.Not.Empty);
        Assert.That(t1, Is.Not.EqualTo(t2));
    }
}
