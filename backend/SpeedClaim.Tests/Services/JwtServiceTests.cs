using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using SpeedClaim.Api.Models;
using SpeedClaim.Api.Services;

namespace SpeedClaim.Tests.Services;

[TestFixture]
public class JwtServiceTests
{
    private Mock<IConfiguration> _configurationMock;
    private Mock<IConfigurationSection> _jwtSettingsMock;
    private JwtService _jwtService;

    [SetUp]
    public void SetUp()
    {
        _configurationMock = new Mock<IConfiguration>();
        _jwtSettingsMock = new Mock<IConfigurationSection>();

        _jwtSettingsMock.Setup(s => s["Secret"]).Returns("SuperSecretKeyThatIsAtLeast32BytesLong!!");
        _jwtSettingsMock.Setup(s => s["Issuer"]).Returns("SpeedClaimApi");
        _jwtSettingsMock.Setup(s => s["Audience"]).Returns("SpeedClaimClients");
        _jwtSettingsMock.Setup(s => s["AccessTokenExpirationMinutes"]).Returns("15");

        _configurationMock.Setup(c => c.GetSection("JwtSettings")).Returns(_jwtSettingsMock.Object);

        _jwtService = new JwtService(_configurationMock.Object);
    }

    [Test]
    public void GenerateAccessToken_WithValidUser_ReturnsValidJwt()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "Test", LastName = "User", Salutation = "Mr.",
            KycStatus = "VERIFIED",
            UserRoles = new List<UserRole>
            {
                new UserRole { Role = new Role { Code = "Customer" } }
            }
        };

        // Act
        var token = _jwtService.GenerateAccessToken(user);

        // Assert
        Assert.That(token, Is.Not.Null.And.Not.Empty);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        Assert.That(jwtToken.Issuer, Is.EqualTo("SpeedClaimApi"));
        Assert.That(jwtToken.Audiences.First(), Is.EqualTo("SpeedClaimClients"));
        Assert.That(jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value, Is.EqualTo(user.Id.ToString()));
        Assert.That(jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email)?.Value, Is.EqualTo("test@example.com"));
        Assert.That(jwtToken.Claims.FirstOrDefault(c => c.Type == "fullName")?.Value, Is.EqualTo("Mr. Test User"));
        Assert.That(jwtToken.Claims.FirstOrDefault(c => c.Type == "kycStatus")?.Value, Is.EqualTo("VERIFIED"));
        Assert.That(jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value, Is.EqualTo("Customer"));
    }

    [Test]
    public void GenerateAccessToken_MissingSecret_ThrowsInvalidOperationException()
    {
        // Arrange
        _jwtSettingsMock.Setup(s => s["Secret"]).Returns((string)null!);
        var user = new User { Id = Guid.NewGuid(), Email = "test@example.com", FirstName = "Test", LastName = "User", Salutation = "Mr." };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _jwtService.GenerateAccessToken(user));
    }

    [Test]
    public void GenerateAccessToken_NoRoles_DefaultsToCustomer()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "Test", LastName = "User", Salutation = "Mr.",
            KycStatus = "PENDING"
        };

        // Act
        var token = _jwtService.GenerateAccessToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        Assert.That(jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value, Is.EqualTo("Customer"));
    }

    [Test]
    public void GenerateRefreshToken_ReturnsBase64String()
    {
        // Act
        var refreshToken = _jwtService.GenerateRefreshToken();

        // Assert
        Assert.That(refreshToken, Is.Not.Null.And.Not.Empty);
        
        // Ensure it's valid base64
        var buffer = new Span<byte>(new byte[refreshToken.Length]);
        var isBase64 = Convert.TryFromBase64String(refreshToken, buffer, out _);
        Assert.That(isBase64, Is.True);
    }
}
