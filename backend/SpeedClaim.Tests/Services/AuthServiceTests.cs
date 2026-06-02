using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using AutoMapper;
using SpeedClaim.Api.Context;
using SpeedClaim.Api.Dtos.Auth;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Mappings;
using SpeedClaim.Api.Models;
using SpeedClaim.Api.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpeedClaim.Tests.Services;

[TestFixture]
public class AuthServiceTests
{
    private SpeedClaimDbContext _context;
    private Mock<IJwtService> _jwtServiceMock;
    private IConfiguration _configuration;
    private IMapper _mapper;
    private AuthService _authService;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<SpeedClaimDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SpeedClaimDbContext(options);

        // Seed roles
        _context.Roles.AddRange(
            new Role { Code = "Customer", Description = "Customer" },
            new Role { Code = "Agent", Description = "Agent" }
        );
        _context.SaveChanges();

        _jwtServiceMock = new Mock<IJwtService>();
        _jwtServiceMock.Setup(j => j.GenerateAccessToken(It.IsAny<User>())).Returns("mock-access-token");
        _jwtServiceMock.Setup(j => j.GenerateRefreshToken()).Returns("mock-refresh-token");

        var inMemorySettings = new Dictionary<string, string?> {
            {"JwtSettings:RefreshTokenExpirationDays", "7"}
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(config => config.AddProfile<MappingProfile>());
        var provider = services.BuildServiceProvider();
        _mapper = provider.GetRequiredService<IMapper>();

        _authService = new AuthService(_context, _jwtServiceMock.Object, _configuration, _mapper);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Test]
    public async Task RegisterUserAsync_WithNewEmail_ReturnsAuthResponse()
    {
        // Arrange
        var request = new RegisterUserRequest(
            "test@user.com",
            "Password123!",
            "Test User",
            "1234567890",
            "123 Test St"
        );

        // Act
        var result = await _authService.RegisterUserAsync(request);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.AccessToken, Is.EqualTo("mock-access-token"));
        Assert.That(result.User.Email, Is.EqualTo("test@user.com"));

        var savedUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "test@user.com");
        Assert.That(savedUser, Is.Not.Null);
        Assert.That(BCrypt.Net.BCrypt.Verify("Password123!", savedUser!.PasswordHash), Is.True);
    }

    [Test]
    public void RegisterUserAsync_WithExistingEmail_ThrowsArgumentException()
    {
        // Arrange
        _context.Users.Add(new User
        {
            Email = "existing@user.com",
            PasswordHash = "hash",
            FullName = "Existing",
            Phone = "1234567890",
            Address = "Address",
            ProfilePictureUrl = "url",
            IsActive = true
        });
        _context.SaveChanges();

        var request = new RegisterUserRequest(
            "existing@user.com",
            "password",
            "Test User",
            "",
            ""
        );

        // Act & Assert
        var ex = Assert.ThrowsAsync<ArgumentException>(async () => await _authService.RegisterUserAsync(request));
        Assert.That(ex.Message, Is.EqualTo("Email is already registered."));
    }

    [Test]
    public async Task LoginAsync_WithValidCredentials_ReturnsAuthResponse()
    {
        // Arrange
        var user = new User
        {
            Email = "login@user.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("ValidPassword1!"),
            FullName = "Login User",
            Phone = "1234567890",
            Address = "Address",
            ProfilePictureUrl = "url",
            IsActive = true
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest(
            "login@user.com",
            "ValidPassword1!"
        );

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.AccessToken, Is.EqualTo("mock-access-token"));
        Assert.That(result.User.Email, Is.EqualTo("login@user.com"));
    }

    [Test]
    public void LoginAsync_WithInvalidPassword_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var user = new User
        {
            Email = "login@user.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("ValidPassword1!"),
            FullName = "Login User",
            Phone = "1234567890",
            Address = "Address",
            ProfilePictureUrl = "url",
            IsActive = true
        };
        _context.Users.Add(user);
        _context.SaveChanges();

        var request = new LoginRequest(
            "login@user.com",
            "WrongPassword"
        );

        // Act & Assert
        var ex = Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await _authService.LoginAsync(request));
        Assert.That(ex.Message, Is.EqualTo("Invalid email or password."));
    }
}
