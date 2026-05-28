using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using SpeedClaim.Core.Dtos.Auth;
using SpeedClaim.Core.Entities.Auth;
using SpeedClaim.Core.Options;
using SpeedClaim.Infrastructure.Data;
using SpeedClaim.Infrastructure.Services;

namespace SpeedClaim.Tests.Services;

[TestFixture]
public class AuthServiceTests
{
    private AppDbContext _context;
    private AuthService _authService;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);

        var jwtOptions = Options.Create(new JwtOptions
        {
            Secret = "SuperSecretKeyForTestingAtLeast256BitsLong!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpiryMinutes = 60,
            RefreshTokenExpiryDays = 7
        });

        _authService = new AuthService(_context, jwtOptions);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task RegisterAsync_ValidRequest_CreatesUserAndReturnsTokens()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "password123",
            FullName = "Test User"
        };

        // Act
        var response = await _authService.RegisterAsync(request, "127.0.0.1");

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response.AccessToken, Is.Not.Empty);
        Assert.That(response.RefreshToken, Is.Not.Empty);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        Assert.That(user, Is.Not.Null);
        Assert.That(user.FullName, Is.EqualTo("Test User"));
        Assert.That(BCrypt.Net.BCrypt.Verify("password123", user.PasswordHash), Is.True);
    }

    [Test]
    public void RegisterAsync_DuplicateEmail_ThrowsArgumentException()
    {
        // Arrange
        _context.Users.Add(new User
        {
            Email = "test@example.com",
            PasswordHash = "hash",
            FullName = "Existing User"
        });
        _context.SaveChanges();

        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "newpassword",
            FullName = "New User"
        };

        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(() => _authService.RegisterAsync(request, "127.0.0.1"));
    }

    [Test]
    public async Task LoginAsync_ValidCredentials_ReturnsTokens()
    {
        // Arrange
        var password = "password123";
        var user = new User
        {
            Email = "login@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            FullName = "Login User"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest
        {
            Email = "login@example.com",
            Password = password
        };

        // Act
        var response = await _authService.LoginAsync(request, "127.0.0.1");

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response.AccessToken, Is.Not.Empty);
        Assert.That(response.RefreshToken, Is.Not.Empty);
    }

    [Test]
    public void LoginAsync_InvalidPassword_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var user = new User
        {
            Email = "login@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            FullName = "Login User"
        };
        _context.Users.Add(user);
        _context.SaveChanges();

        var request = new LoginRequest
        {
            Email = "login@example.com",
            Password = "wrongpassword"
        };

        // Act & Assert
        Assert.ThrowsAsync<UnauthorizedAccessException>(() => _authService.LoginAsync(request, "127.0.0.1"));
    }
}
