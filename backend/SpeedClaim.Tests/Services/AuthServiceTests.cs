using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Moq;
using NUnit.Framework;
using SpeedClaim.Api.Dtos.Auth;
using SpeedClaim.Api.Dtos.Common;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;
using SpeedClaim.Api.Models.Enums;
using SpeedClaim.Api.Services;

namespace SpeedClaim.Tests.Services;

[TestFixture]
public class AuthServiceTests
{
    private Mock<IUnitOfWork> _mockUnitOfWork = null!;
    private Mock<IUserRepository> _mockUserRepository = null!;
    private Mock<IRepository<Customer>> _mockCustomerRepository = null!;
    private Mock<IRepository<Session>> _mockSessionRepository = null!;
    private Mock<IRepository<UserToken>> _mockUserTokenRepository = null!;
    private Mock<IJwtService> _mockJwtService = null!;
    private Mock<IEmailService> _mockEmailService = null!;
    private AuthService _authService = null!;

    [SetUp]
    public void Setup()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockCustomerRepository = new Mock<IRepository<Customer>>();
        _mockSessionRepository = new Mock<IRepository<Session>>();
        _mockUserTokenRepository = new Mock<IRepository<UserToken>>();
        _mockJwtService = new Mock<IJwtService>();
        _mockEmailService = new Mock<IEmailService>();

        _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);
        _mockUnitOfWork.Setup(u => u.Customers).Returns(_mockCustomerRepository.Object);
        _mockUnitOfWork.Setup(u => u.Sessions).Returns(_mockSessionRepository.Object);
        _mockUnitOfWork.Setup(u => u.UserTokens).Returns(_mockUserTokenRepository.Object);
        _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);
        _mockUnitOfWork.Setup(u => u.AuditLogs).Returns(new Mock<IRepository<AuditLog>>().Object);
        _mockUnitOfWork.Setup(u => u.UserConsents).Returns(new Mock<IRepository<UserConsent>>().Object);
        _mockUnitOfWork.Setup(u => u.Addresses).Returns(new Mock<IRepository<Address>>().Object);

        var mockHttpContext = new Mock<IHttpContextAccessor>();
        mockHttpContext.Setup(h => h.HttpContext).Returns((HttpContext?)null);

        _authService = new AuthService(_mockUnitOfWork.Object, _mockJwtService.Object, _mockEmailService.Object, Mock.Of<Microsoft.Extensions.Logging.ILogger<AuthService>>(), mockHttpContext.Object);
    }

    [Test]
    public void RegisterCustomerAsync_EmailExists_ThrowsException()
    {
        var address = new AddressDto("123 St", null, "City", "State", "12345", "Country");
        var request = new RegisterUserRequest("test@test.com", "pass", "Mr", "John", "Doe", "123", address, null, true, DateOnly.FromDateTime(DateTime.UtcNow), "A", "P", Gender.Male, MaritalStatus.Single);
        
        _mockUserRepository.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(new User());

        var ex = Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ConflictException>(() => _authService.RegisterCustomerAsync(request));
        Assert.That(ex.Message, Is.EqualTo("Email already registered"));
    }

    [Test]
    public async Task RegisterCustomerAsync_ValidRequest_Success()
    {
        var address = new AddressDto("123 St", null, "City", "State", "12345", "Country");
        var request = new RegisterUserRequest("new@test.com", "pass", "Mr", "John", "Doe", "123", address, null, true, DateOnly.FromDateTime(DateTime.UtcNow), "A", "P", Gender.Male, MaritalStatus.Single);
        
        _mockUserRepository.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync((User?)null);
        _mockJwtService.Setup(j => j.GenerateRefreshToken()).Returns("verify_token");

        var result = await _authService.RegisterCustomerAsync(request);

        Assert.That(result.Email, Is.EqualTo("new@test.com"));
        Assert.That(result.Role, Is.EqualTo("Customer"));
        
        _mockUserRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
        _mockCustomerRepository.Verify(r => r.AddAsync(It.IsAny<Customer>()), Times.Once);
        _mockUserTokenRepository.Verify(r => r.AddAsync(It.IsAny<UserToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
        _mockEmailService.Verify(e => e.SendEmailVerificationAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Test]
    public void LoginAsync_UserNotFound_ThrowsException()
    {
        var request = new LoginRequest("none@test.com", "password");
        _mockUserRepository.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync((User?)null);

        var ex = Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ValidationException>(() => _authService.LoginAsync(request));
        Assert.That(ex.Message, Is.EqualTo("Invalid email or password."));
    }

    [Test]
    public void LoginAsync_InvalidPassword_ThrowsException()
    {
        var request = new LoginRequest("test@test.com", "wrongpassword");
        var user = new User { PasswordHash = BCrypt.Net.BCrypt.HashPassword("correctpassword") };

        _mockUserRepository.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(user);

        var ex = Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ValidationException>(() => _authService.LoginAsync(request));
        Assert.That(ex.Message, Is.EqualTo("Invalid email or password."));
    }

    [Test]
    public void LoginAsync_UserInactive_ThrowsException()
    {
        var request = new LoginRequest("test@test.com", "password");
        var user = new User
        {
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
            IsActive = false
        };

        _mockUserRepository.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(user);

        var ex = Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ForbiddenException>(() => _authService.LoginAsync(request));
        Assert.That(ex.Message, Is.EqualTo("Account is inactive"));
    }

    [Test]
    public void LoginAsync_UnverifiedEmail_ThrowsUnprocessableException()
    {
        var request = new LoginRequest("test@test.com", "password");
        var user = new User
        {
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
            IsActive = true,
            IsEmailVerified = false
        };

        _mockUserRepository.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(user);

        var ex = Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.UnprocessableException>(() => _authService.LoginAsync(request));
        Assert.That(ex.Message, Is.EqualTo("Please verify your email address before signing in."));
    }

    [Test]
    public async Task LoginAsync_ValidCredentials_Success()
    {
        var request = new LoginRequest("test@test.com", "password");
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
            IsActive = true,
            IsEmailVerified = true,
            Role = UserRole.Customer,
            FirstName = "Jane",
            LastName = "Doe",
            Salutation = Salutation.Ms
        };
        
        _mockUserRepository.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(user);
        
        _mockJwtService.Setup(j => j.GenerateAccessToken(It.IsAny<User>(), It.IsAny<Guid>())).Returns("access_token");
        _mockJwtService.Setup(j => j.GenerateRefreshToken()).Returns("refresh_token");

        var result = await _authService.LoginAsync(request);

        Assert.That(result.AccessToken, Is.EqualTo("access_token"));
        Assert.That(result.RefreshToken, Does.EndWith(":refresh_token"));
        Assert.That(result.User.Email, Is.EqualTo("test@test.com"));
        
        _mockSessionRepository.Verify(r => r.AddAsync(It.IsAny<Session>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public void RefreshTokenAsync_InvalidToken_ThrowsException()
    {
        var request = new RefreshTokenRequest { RefreshToken = "invalid" };
        _mockSessionRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Session, bool>>>()))
            .ReturnsAsync(new List<Session>());

        var ex = Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ValidationException>(() => _authService.RefreshTokenAsync(request));
        Assert.That(ex.Message, Is.EqualTo("Invalid refresh token format"));
    }

    [Test]
    public async Task RefreshTokenAsync_ValidToken_Success()
    {
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var session = new Session
        {
            Id = sessionId,
            UserId = userId,
            RefreshTokenHash = BCrypt.Net.BCrypt.HashPassword("valid_refresh"),
            IsRevoked = false,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };
        // New token format: "{sessionId}:{rawToken}"
        var request = new RefreshTokenRequest { RefreshToken = $"{sessionId}:valid_refresh" };

        _mockSessionRepository.Setup(r => r.GetByIdAsync(sessionId)).ReturnsAsync(session);

        var user = new User { Id = userId, IsActive = true, Email = "test@test.com", Role = UserRole.Customer };
        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        _mockJwtService.Setup(j => j.GenerateAccessToken(It.IsAny<User>(), It.IsAny<Guid>())).Returns("new_access");
        _mockJwtService.Setup(j => j.GenerateRefreshToken()).Returns("new_refresh");

        var result = await _authService.RefreshTokenAsync(request);

        Assert.That(session.IsRevoked, Is.True);
        Assert.That(result.AccessToken, Is.EqualTo("new_access"));
        Assert.That(result.RefreshToken, Does.EndWith(":new_refresh"));

        _mockSessionRepository.Verify(r => r.AddAsync(It.IsAny<Session>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }
    
    [Test]
    public void VerifyEmailAsync_InvalidToken_ThrowsException()
    {
        _mockUserTokenRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<UserToken, bool>>>()))
            .ReturnsAsync(new List<UserToken>());
            
        var ex = Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ValidationException>(() => _authService.VerifyEmailAsync("invalid"));
        Assert.That(ex.Message, Is.EqualTo("Invalid or expired token"));
    }

    [Test]
    public async Task VerifyEmailAsync_ValidToken_Success()
    {
        var userId = Guid.NewGuid();
        var tokenId = Guid.NewGuid();
        var userToken = new UserToken
        {
            Id = tokenId,
            UserId = userId,
            TokenType = TokenType.EmailVerification,
            TokenHash = BCrypt.Net.BCrypt.HashPassword("valid_token"),
            IsUsed = false,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };
        // New token format: "{tokenId}:{rawToken}"
        var token = $"{tokenId}:valid_token";

        _mockUserTokenRepository.Setup(r => r.GetByIdAsync(tokenId)).ReturnsAsync(userToken);

        var user = new User { Id = userId, IsEmailVerified = false };
        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        await _authService.VerifyEmailAsync(token);
        
        Assert.That(userToken.IsUsed, Is.True);
        Assert.That(user.IsEmailVerified, Is.True);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public async Task LogoutAsync_Success()
    {
        var userId = Guid.NewGuid();
        var session1 = new Session { IsRevoked = false };
        var session2 = new Session { IsRevoked = false };
        
        _mockSessionRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Session, bool>>>()))
            .ReturnsAsync(new List<Session> { session1, session2 });
            
        await _authService.LogoutAsync(userId.ToString());
        
        Assert.That(session1.IsRevoked, Is.True);
        Assert.That(session2.IsRevoked, Is.True);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public async Task ForgotPasswordAsync_UserNotFound_SilentSuccess()
    {
        _mockUserRepository.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync((User?)null);
            
        await _authService.ForgotPasswordAsync("none@test.com");
        
        _mockUserTokenRepository.Verify(r => r.AddAsync(It.IsAny<UserToken>()), Times.Never);
    }

    [Test]
    public async Task ForgotPasswordAsync_ValidUser_Success()
    {
        var user = new User { Email = "test@test.com" };
        var oldToken = new UserToken { IsUsed = false };
        _mockUserRepository.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(user);
        _mockUserTokenRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<UserToken, bool>>>()))
            .ReturnsAsync(new List<UserToken> { oldToken });
        _mockJwtService.Setup(j => j.GenerateRefreshToken()).Returns("reset_token");

        await _authService.ForgotPasswordAsync("test@test.com");
        
        Assert.That(oldToken.IsUsed, Is.True);
        _mockUserTokenRepository.Verify(r => r.AddAsync(It.IsAny<UserToken>()), Times.Once);
        _mockEmailService.Verify(e => e.SendPasswordResetAsync("test@test.com", It.IsAny<string>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public void ResetPasswordCustomerAsync_InvalidToken_ThrowsException()
    {
        var request = new ResetPasswordRequest { Token = "invalid", NewPassword = "new" };
        _mockUserTokenRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<UserToken, bool>>>()))
            .ReturnsAsync(new List<UserToken>());
            
        var ex = Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ValidationException>(() => _authService.ResetPasswordCustomerAsync(request));
        Assert.That(ex.Message, Is.EqualTo("Invalid or expired token"));
    }

    [Test]
    public async Task ResetPasswordCustomerAsync_ValidToken_Success()
    {
        var userId = Guid.NewGuid();
        var tokenId = Guid.NewGuid();
        var userToken = new UserToken
        {
            Id = tokenId,
            UserId = userId,
            TokenType = TokenType.PasswordReset,
            TokenHash = BCrypt.Net.BCrypt.HashPassword("valid"),
            IsUsed = false,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };
        // New token format: "{tokenId}:{rawToken}"
        var request = new ResetPasswordRequest { Token = $"{tokenId}:valid", NewPassword = "newpassword" };

        _mockUserTokenRepository.Setup(r => r.GetByIdAsync(tokenId)).ReturnsAsync(userToken);

        var session = new Session { UserId = userId, IsRevoked = false };
        var user = new User
        {
            Id = userId,
            PasswordHash = "old",
            FailedLoginAttempts = 5,
            LockedUntil = DateTime.UtcNow.AddMinutes(10)
        };
        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockSessionRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Session, bool>>>()))
            .ReturnsAsync(new List<Session> { session });

        await _authService.ResetPasswordCustomerAsync(request);
        
        Assert.That(userToken.IsUsed, Is.True);
        Assert.That(BCrypt.Net.BCrypt.Verify("newpassword", user.PasswordHash), Is.True);
        Assert.That(user.FailedLoginAttempts, Is.EqualTo(0));
        Assert.That(user.LockedUntil, Is.Null);
        Assert.That(session.IsRevoked, Is.True);
        Assert.That(session.UpdatedAt, Is.Not.Null);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public void ResetPasswordAsync_AdminReset_UserNotFound_ThrowsException()
    {
        _mockUserRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);
        
        var ex = Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.NotFoundException>(() => _authService.ResetPasswordAsync(Guid.NewGuid().ToString(), "new", "admin"));
        Assert.That(ex.Message, Is.EqualTo("User not found"));
    }

    [Test]
    public async Task ResetPasswordAsync_AdminReset_Success()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            PasswordHash = "old",
            FailedLoginAttempts = 5,
            LockedUntil = DateTime.UtcNow.AddMinutes(10)
        };
        var session = new Session { UserId = user.Id, IsRevoked = false };
        _mockUserRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(user);
        _mockSessionRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Session, bool>>>()))
            .ReturnsAsync(new List<Session> { session });
        
        await _authService.ResetPasswordAsync(user.Id.ToString(), "newpassword", Guid.NewGuid().ToString());
        
        Assert.That(BCrypt.Net.BCrypt.Verify("newpassword", user.PasswordHash), Is.True);
        Assert.That(user.FailedLoginAttempts, Is.EqualTo(0));
        Assert.That(user.LockedUntil, Is.Null);
        Assert.That(session.IsRevoked, Is.True);
        Assert.That(session.UpdatedAt, Is.Not.Null);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public async Task RegisterAgentAsync_NewEmail_CreatesAgent()
    {
        _mockUserRepository.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync((User?)null);
        var mockAgentRepo = new Mock<IRepository<Agent>>();
        _mockUnitOfWork.Setup(u => u.Agents).Returns(mockAgentRepo.Object);
        _mockJwtService.Setup(j => j.GenerateRefreshToken()).Returns("agent_reset_token");

        var addr = new AddressDto("123 Main St", null, "Mumbai", "MH", "400001", "India");
        var request = new RegisterAgentRequest("agent@test.com", "Mr", "Test", "Agent", "9999999999", addr, null, false, "LIC-001", DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)), "TestAgency", "123456789012", "ABCDE1234F", MaritalStatus.Single);

        var result = await _authService.RegisterAgentAsync(request, Guid.NewGuid().ToString());

        Assert.That(result.Email, Is.EqualTo("agent@test.com"));
        Assert.That(result.Role, Is.EqualTo("Agent"));
        mockAgentRepo.Verify(r => r.AddAsync(It.IsAny<Agent>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.AtLeastOnce);
        _mockUserTokenRepository.Verify(r => r.AddAsync(It.IsAny<UserToken>()), Times.Once);
        _mockEmailService.Verify(e => e.SendAgentWelcomeAsync("agent@test.com", "Test", It.IsAny<string>()), Times.Once);
    }

    [Test]
    public void RegisterAgentAsync_EmailAlreadyExists_ThrowsException()
    {
        _mockUserRepository.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(new User { Email = "agent@test.com" });
        var mockAgentRepo = new Mock<IRepository<Agent>>();
        _mockUnitOfWork.Setup(u => u.Agents).Returns(mockAgentRepo.Object);

        var addr = new AddressDto("1 St", null, "City", "State", "000001", "India");
        var request = new RegisterAgentRequest("agent@test.com", "Mr", "T", "A", "1", addr, null, false, "L", DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)), "Agency", "000000000000", "AAAAA0000A", MaritalStatus.Single);
        var ex = Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ConflictException>(() => _authService.RegisterAgentAsync(request, Guid.NewGuid().ToString()));
        Assert.That(ex.Message, Is.EqualTo("Email already registered"));
    }

    [Test]
    public async Task AddCustomerAsync_NewEmail_CreatesCustomerTaggedToAgent()
    {
        var agentUserId = Guid.NewGuid();
        var agent = new Agent { Id = Guid.NewGuid(), UserId = agentUserId };
        var mockAgentRepo = new Mock<IRepository<Agent>>();
        mockAgentRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Agent, bool>>>())).ReturnsAsync(agent);
        _mockUnitOfWork.Setup(u => u.Agents).Returns(mockAgentRepo.Object);
        _mockUserRepository.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync((User?)null);
        _mockJwtService.Setup(j => j.GenerateRefreshToken()).Returns("customer_reset_token");

        Customer? savedCustomer = null;
        _mockCustomerRepository.Setup(r => r.AddAsync(It.IsAny<Customer>()))
            .Callback<Customer>(c => savedCustomer = c)
            .Returns(Task.CompletedTask);

        var addr = new AddressDto("1 Main St", null, "Mumbai", "MH", "400001", "India");
        var request = new AgentAddCustomerRequest(
            "cust@test.com", "Mr", "Rahul", "Verma", "9999999999",
            addr, null, true, DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25)),
            Gender.Male, MaritalStatus.Single);

        var result = await _authService.AddCustomerAsync(request, agentUserId.ToString());

        Assert.That(result.Email, Is.EqualTo("cust@test.com"));
        Assert.That(result.Role, Is.EqualTo("Customer"));
        Assert.That(savedCustomer, Is.Not.Null);
        Assert.That(savedCustomer!.OnboardingAgentId, Is.EqualTo(agent.Id));
        _mockUserTokenRepository.Verify(r => r.AddAsync(It.IsAny<UserToken>()), Times.Once);
        _mockEmailService.Verify(e => e.SendCustomerWelcomeAsync("cust@test.com", "Rahul", It.IsAny<string>()), Times.Once);
    }

    [Test]
    public void AddCustomerAsync_EmailAlreadyExists_ThrowsException()
    {
        var agentUserId = Guid.NewGuid();
        var agent = new Agent { Id = Guid.NewGuid(), UserId = agentUserId };
        var mockAgentRepo = new Mock<IRepository<Agent>>();
        mockAgentRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Agent, bool>>>())).ReturnsAsync(agent);
        _mockUnitOfWork.Setup(u => u.Agents).Returns(mockAgentRepo.Object);
        _mockUserRepository.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(new User { Email = "cust@test.com" });

        var addr = new AddressDto("1 Main St", null, "Mumbai", "MH", "400001", "India");
        var request = new AgentAddCustomerRequest(
            "cust@test.com", "Mr", "Rahul", "Verma", "9999999999",
            addr, null, true, DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25)),
            Gender.Male, MaritalStatus.Single);

        var ex = Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ConflictException>(() => _authService.AddCustomerAsync(request, agentUserId.ToString()));
        Assert.That(ex.Message, Is.EqualTo("Email already registered"));
    }

    [Test]
    public void AddCustomerAsync_AgentNotFound_ThrowsNotFoundException()
    {
        var mockAgentRepo = new Mock<IRepository<Agent>>();
        mockAgentRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Agent, bool>>>())).ReturnsAsync((Agent?)null);
        _mockUnitOfWork.Setup(u => u.Agents).Returns(mockAgentRepo.Object);

        var addr = new AddressDto("1 Main St", null, "Mumbai", "MH", "400001", "India");
        var request = new AgentAddCustomerRequest(
            "cust@test.com", "Mr", "Rahul", "Verma", "9999999999",
            addr, null, true, DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25)),
            Gender.Male, MaritalStatus.Single);

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.NotFoundException>(() => _authService.AddCustomerAsync(request, Guid.NewGuid().ToString()));
    }

    [Test]
    public void RefreshTokenAsync_SessionNullOrExpired_ThrowsException()
    {
        var sessionId = Guid.NewGuid();
        var request = new RefreshTokenRequest { RefreshToken = $"{sessionId}:sometoken" };
        _mockSessionRepository.Setup(r => r.GetByIdAsync(sessionId)).ReturnsAsync((Session?)null);

        var ex = Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ValidationException>(() => _authService.RefreshTokenAsync(request));
        Assert.That(ex.Message, Is.EqualTo("Invalid or expired refresh token"));
    }

    [Test]
    public void RefreshTokenAsync_BcryptVerifyFails_ThrowsException()
    {
        var sessionId = Guid.NewGuid();
        var session = new Session
        {
            Id = sessionId,
            UserId = Guid.NewGuid(),
            RefreshTokenHash = BCrypt.Net.BCrypt.HashPassword("correct_token"),
            IsRevoked = false,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };
        _mockSessionRepository.Setup(r => r.GetByIdAsync(sessionId)).ReturnsAsync(session);
        var request = new RefreshTokenRequest { RefreshToken = $"{sessionId}:wrong_token" };

        var ex = Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ValidationException>(() => _authService.RefreshTokenAsync(request));
        Assert.That(ex.Message, Is.EqualTo("Invalid refresh token"));
    }

    [Test]
    public void RefreshTokenAsync_UserInactive_ThrowsException()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var rawToken = "my_token";
        var session = new Session
        {
            Id = sessionId,
            UserId = userId,
            RefreshTokenHash = BCrypt.Net.BCrypt.HashPassword(rawToken),
            IsRevoked = false,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };
        _mockSessionRepository.Setup(r => r.GetByIdAsync(sessionId)).ReturnsAsync(session);
        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new User { Id = userId, IsActive = false });
        var request = new RefreshTokenRequest { RefreshToken = $"{sessionId}:{rawToken}" };

        var ex = Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ForbiddenException>(() => _authService.RefreshTokenAsync(request));
        Assert.That(ex.Message, Is.EqualTo("User inactive or not found"));
    }

    [Test]
    public void VerifyEmailAsync_TokenNullOrExpired_ThrowsException()
    {
        var tokenId = Guid.NewGuid();
        _mockUserTokenRepository.Setup(r => r.GetByIdAsync(tokenId)).ReturnsAsync((UserToken?)null);

        var ex = Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ValidationException>(() => _authService.VerifyEmailAsync($"{tokenId}:tok"));
        Assert.That(ex.Message, Is.EqualTo("Invalid or expired token"));
    }

    [Test]
    public void VerifyEmailAsync_WrongTokenType_ThrowsException()
    {
        var tokenId = Guid.NewGuid();
        var rawToken = "tok";
        var userToken = new UserToken
        {
            Id = tokenId,
            TokenType = TokenType.PasswordReset,
            TokenHash = BCrypt.Net.BCrypt.HashPassword(rawToken),
            IsUsed = false,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };
        _mockUserTokenRepository.Setup(r => r.GetByIdAsync(tokenId)).ReturnsAsync(userToken);

        var ex = Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ValidationException>(() => _authService.VerifyEmailAsync($"{tokenId}:{rawToken}"));
        Assert.That(ex.Message, Is.EqualTo("Invalid token type"));
    }

    [Test]
    public void VerifyEmailAsync_BcryptVerifyFails_ThrowsException()
    {
        var tokenId = Guid.NewGuid();
        var userToken = new UserToken
        {
            Id = tokenId,
            TokenType = TokenType.EmailVerification,
            TokenHash = BCrypt.Net.BCrypt.HashPassword("correct"),
            IsUsed = false,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };
        _mockUserTokenRepository.Setup(r => r.GetByIdAsync(tokenId)).ReturnsAsync(userToken);

        var ex = Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ValidationException>(() => _authService.VerifyEmailAsync($"{tokenId}:wrong"));
        Assert.That(ex.Message, Is.EqualTo("Invalid or expired token"));
    }

    [Test]
    public void ResetPasswordCustomerAsync_WrongTokenType_ThrowsException()
    {
        var tokenId = Guid.NewGuid();
        var rawToken = "tok";
        var userToken = new UserToken
        {
            Id = tokenId,
            TokenType = TokenType.EmailVerification,
            TokenHash = BCrypt.Net.BCrypt.HashPassword(rawToken),
            IsUsed = false,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };
        _mockUserTokenRepository.Setup(r => r.GetByIdAsync(tokenId)).ReturnsAsync(userToken);

        var request = new ResetPasswordRequest { Token = $"{tokenId}:{rawToken}", NewPassword = "new" };
        var ex = Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ValidationException>(() => _authService.ResetPasswordCustomerAsync(request));
        Assert.That(ex.Message, Is.EqualTo("Invalid token type"));
    }

    [Test]
    public void ResetPasswordCustomerAsync_BcryptVerifyFails_ThrowsException()
    {
        var tokenId = Guid.NewGuid();
        var userToken = new UserToken
        {
            Id = tokenId,
            TokenType = TokenType.PasswordReset,
            TokenHash = BCrypt.Net.BCrypt.HashPassword("correct"),
            IsUsed = false,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };
        _mockUserTokenRepository.Setup(r => r.GetByIdAsync(tokenId)).ReturnsAsync(userToken);

        var request = new ResetPasswordRequest { Token = $"{tokenId}:wrong", NewPassword = "new" };
        var ex = Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ValidationException>(() => _authService.ResetPasswordCustomerAsync(request));
        Assert.That(ex.Message, Is.EqualTo("Invalid or expired token"));
    }

    [Test]
    public void LoginAsync_LockedAccount_ThrowsForbiddenException()
    {
        var request = new LoginRequest("locked@test.com", "password");
        var user = new User
        {
            Email = "locked@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
            IsActive = true,
            LockedUntil = DateTime.UtcNow.AddMinutes(10)
        };
        _mockUserRepository.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(user);

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ForbiddenException>(() => _authService.LoginAsync(request));
    }

    [Test]
    public async Task LoginAsync_WrongPasswordAtLockoutThreshold_SetsLockedUntil()
    {
        var request = new LoginRequest("test@test.com", "wrongpassword");
        var user = new User
        {
            Email = "test@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correctpassword"),
            IsActive = true,
            FailedLoginAttempts = 4 // one more failure triggers lockout (maxAttempts = 5)
        };
        _mockUserRepository.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(user);

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ValidationException>(() => _authService.LoginAsync(request));
        await _mockUnitOfWork.Object.CompleteAsync();

        Assert.That(user.FailedLoginAttempts, Is.EqualTo(5));
        Assert.That(user.LockedUntil, Is.Not.Null);
        Assert.That(user.LockedUntil, Is.GreaterThan(DateTime.UtcNow));
    }

    [Test]
    public async Task ResendVerificationEmailAsync_UserNotFound_SilentSuccess()
    {
        _mockUserRepository.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync((User?)null);

        await _authService.ResendVerificationEmailAsync("none@test.com");

        _mockUserTokenRepository.Verify(r => r.AddAsync(It.IsAny<UserToken>()), Times.Never);
        _mockEmailService.Verify(e => e.SendEmailVerificationAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task ResendVerificationEmailAsync_AlreadyVerified_SilentSuccess()
    {
        _mockUserRepository.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(new User { IsEmailVerified = true });

        await _authService.ResendVerificationEmailAsync("verified@test.com");

        _mockUserTokenRepository.Verify(r => r.AddAsync(It.IsAny<UserToken>()), Times.Never);
        _mockEmailService.Verify(e => e.SendEmailVerificationAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task ResendVerificationEmailAsync_ValidUnverifiedUser_SendsEmail()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "unverified@test.com", IsEmailVerified = false };
        _mockUserRepository.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(user);
        _mockUserTokenRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<UserToken, bool>>>()))
            .ReturnsAsync(new List<UserToken>());
        _mockJwtService.Setup(j => j.GenerateRefreshToken()).Returns("new_verify_token");

        await _authService.ResendVerificationEmailAsync("unverified@test.com");

        _mockUserTokenRepository.Verify(r => r.AddAsync(It.IsAny<UserToken>()), Times.Once);
        _mockEmailService.Verify(e => e.SendEmailVerificationAsync("unverified@test.com", It.IsAny<string>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }
}
