using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Moq;
using SpeedClaim.Api.Dtos.Auth;
using SpeedClaim.Api.Models;
using SpeedClaim.Api.Services;
using SpeedClaim.Api.Interfaces;
using AutoMapper;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace SpeedClaim.Tests.Services;

[TestFixture]
public class AuthServiceTests
{
    private Mock<IUnitOfWork> _mockUnitOfWork;
    private Mock<IUserRepository> _mockUserRepo;
    private Mock<IRepository<Role>> _mockRoleRepo;
    private Mock<IRefreshTokenRepository> _mockRefreshTokenRepo;
    private Mock<IEmailService> _mockEmailService;
    private Mock<IJwtService> _mockJwtService;
    private Mock<IMapper> _mockMapper;
    private Mock<IConfiguration> _mockConfig;
    private AuthService _authService;

    [SetUp]
    public void Setup()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockUserRepo = new Mock<IUserRepository>();
        _mockRoleRepo = new Mock<IRepository<Role>>();
        _mockRefreshTokenRepo = new Mock<IRefreshTokenRepository>();

        _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepo.Object);
        _mockUnitOfWork.Setup(u => u.Roles).Returns(_mockRoleRepo.Object);
        _mockUnitOfWork.Setup(u => u.RefreshTokens).Returns(_mockRefreshTokenRepo.Object);

        _mockConfig = new Mock<IConfiguration>();
        _mockConfig = new Mock<IConfiguration>();
        var jwtSection = new Mock<IConfigurationSection>();
        jwtSection.Setup(x => x["Key"]).Returns("SuperSecretKeyThatIsAtLeast32BytesLong!");
        jwtSection.Setup(x => x["Issuer"]).Returns("SpeedClaim");
        jwtSection.Setup(x => x["Audience"]).Returns("SpeedClaimUsers");
        jwtSection.Setup(x => x["DurationInMinutes"]).Returns("60");
        _mockConfig.Setup(c => c.GetSection("JwtSettings")).Returns(jwtSection.Object);

        _mockEmailService = new Mock<IEmailService>();
        _mockJwtService = new Mock<IJwtService>();
        _mockMapper = new Mock<IMapper>();

        _authService = new AuthService(_mockUnitOfWork.Object, _mockJwtService.Object, _mockConfig.Object, _mockMapper.Object, _mockEmailService.Object);
    }

    [Test]
    public async Task RegisterUserAsync_ValidRequest_CreatesUser()
    {
        var request = new RegisterUserRequest(
            "test@example.com", 
            "Password123!", 
            "John Doe", 
            "1234567890", 
            new SpeedClaim.Api.Dtos.Common.AddressDto("123 Main St", "City", "State", "12345", "Country"), 
            DateTime.UtcNow.AddYears(-30), 
            "123412341234", 
            "ABCDE1234F", 
            SpeedClaim.Api.Models.Enums.Gender.Male
        );
        
        _mockUserRepo.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(false);
        _mockRoleRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Role, bool>>>())).ReturnsAsync(new Role { Id = Guid.NewGuid(), Code = "Customer" });
        _mockUserRepo.Setup(r => r.AddAsync(It.IsAny<User>())).Callback<User>(u => u.Id = Guid.NewGuid());
        
        var result = await _authService.RegisterUserAsync(request);

        Assert.That(result, Is.Not.Null);
        _mockUserRepo.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Exactly(2));
    }

    [Test]
    public void RegisterUserAsync_DuplicateEmail_ThrowsArgumentException()
    {
        var request = new RegisterUserRequest(
            "test@example.com", 
            "Password123!", 
            "John Doe", 
            "1234567890", 
            new SpeedClaim.Api.Dtos.Common.AddressDto("123 Main St", "City", "State", "12345", "Country"), 
            DateTime.UtcNow.AddYears(-30), 
            "123412341234", 
            "ABCDE1234F", 
            SpeedClaim.Api.Models.Enums.Gender.Male
        );
        
        _mockUserRepo.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(true);

        var ex = Assert.ThrowsAsync<ArgumentException>(async () => await _authService.RegisterUserAsync(request));
        Assert.That(ex.Message, Does.Contain("Email is already registered"));
    }

    [Test]
    public async Task LoginAsync_ValidCredentials_ReturnsAuthResponse()
    {
        var email = "test@example.com";
        var password = "Password123!";
        var user = new User 
        { 
            Id = Guid.NewGuid(), 
            Email = email, 
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            UserRoles = new List<UserRole> { new UserRole { Role = new Role { Code = "Customer" } } }
        };

        _mockUserRepo.Setup(r => r.GetUserByEmailWithRolesAsync(email)).ReturnsAsync(user);

        var request = new LoginRequest(email, password);
        var result = await _authService.LoginAsync(request);

        Assert.That(result, Is.Not.Null);
        _mockRefreshTokenRepo.Verify(r => r.AddAsync(It.IsAny<RefreshToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public void LoginAsync_InvalidEmail_ThrowsUnauthorizedAccessException()
    {
        _mockUserRepo.Setup(r => r.GetUserByEmailWithRolesAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var request = new LoginRequest("wrong@example.com", "pass");

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await _authService.LoginAsync(request));
    }
}
