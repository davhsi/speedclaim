using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SpeedClaim.Api.Dtos.Auth;
using SpeedClaim.Api.Exceptions;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;
using SpeedClaim.Api.Models.Enums;

namespace SpeedClaim.Api.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtService _jwtService;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthService(IUnitOfWork unitOfWork, IJwtService jwtService, IEmailService emailService, ILogger<AuthService> logger, IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
        _emailService = emailService;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<RegistrationResponse> RegisterCustomerAsync(RegisterUserRequest request)
    {
        var existingUser = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (existingUser != null)
            throw new ConflictException("Email already registered");

        var salutation = Enum.TryParse<Salutation>(request.Salutation, ignoreCase: true, out var sal) ? sal : Salutation.Mr;

        var user = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = UserRole.Customer,
            IsEmailVerified = false,
            IsActive = true,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            Salutation = salutation
        };

        var customer = new Customer
        {
            User = user,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender
        };

        var ip = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.Customers.AddAsync(customer);
        await _unitOfWork.AuditLogs.AddAsync(new AuditLog
        {
            Id = Guid.NewGuid(), UserId = user.Id, EntityType = "User", EntityId = user.Id,
            Action = "CustomerRegistered", NewValue = user.Email, IpAddress = ip, CreatedAt = DateTime.UtcNow
        });

        // Record DPDP Act 2023 consent — user accepted data processing and KYC collection at registration
        foreach (var consentType in new[] { "DataProcessing", "KycDataCollection" })
        {
            await _unitOfWork.UserConsents.AddAsync(new UserConsent
            {
                Id = Guid.NewGuid(), UserId = user.Id,
                ConsentType = consentType, IsGranted = true,
                ConsentVersion = "1.0", IpAddress = ip,
                ConsentedAt = DateTimeOffset.UtcNow
            });
        }

        var rawToken = _jwtService.GenerateRefreshToken();
        var userToken = new UserToken
        {
            Id = Guid.NewGuid(),
            User = user,
            TokenType = TokenType.EmailVerification,
            TokenHash = BCrypt.Net.BCrypt.HashPassword(rawToken),
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };
        await _unitOfWork.UserTokens.AddAsync(userToken);
        await _unitOfWork.CompleteAsync();

        // Send "{tokenId}:{rawToken}" — lets VerifyEmailAsync skip the full-table scan
        var verificationPayload = $"{userToken.Id}:{rawToken}";
        await _emailService.SendEmailVerificationAsync(user.Email, verificationPayload);

        _logger.LogInformation("New customer registered: {Email}", request.Email);
        return new RegistrationResponse(user.Email, user.Role.ToString());
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        const int maxAttempts = 5;
        const int lockoutMinutes = 15;

        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        // Check lockout before verifying password to avoid timing leaks
        if (user != null && user.LockedUntil.HasValue && user.LockedUntil > DateTime.UtcNow)
        {
            _logger.LogWarning("Login attempt on locked account: {Email}", request.Email);
            throw new ForbiddenException($"Account is temporarily locked. Try again after {user.LockedUntil:HH:mm} UTC.");
        }

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            if (user != null)
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= maxAttempts)
                {
                    user.LockedUntil = DateTime.UtcNow.AddMinutes(lockoutMinutes);
                    _logger.LogWarning("Account locked after {Max} failed attempts: {Email}", maxAttempts, request.Email);
                }
                await _unitOfWork.CompleteAsync();
            }
            _logger.LogWarning("Failed login attempt for email: {Email}", request.Email);
            throw new ValidationException("Invalid credentials");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login attempt for inactive account: {Email}", request.Email);
            throw new ForbiddenException("Account is inactive");
        }

        // Successful login — reset lockout state
        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;

        var accessToken = _jwtService.GenerateAccessToken(user);
        var rawRefreshToken = _jwtService.GenerateRefreshToken();

        var session = new Session
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            RefreshTokenHash = BCrypt.Net.BCrypt.HashPassword(rawRefreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IpAddress = "Unknown",
            UserAgent = "Unknown"
        };

        await _unitOfWork.Sessions.AddAsync(session);
        user.LastLoginAt = DateTime.UtcNow;
        await _unitOfWork.AuditLogs.AddAsync(new AuditLog
        {
            Id = Guid.NewGuid(), UserId = user.Id, EntityType = "Session", EntityId = session.Id,
            Action = "UserLoggedIn", NewValue = user.Role.ToString(), CreatedAt = DateTime.UtcNow
        });
        await _unitOfWork.CompleteAsync();

        // Return "{sessionId}:{rawToken}" so refresh can target the specific session directly
        var refreshTokenPayload = $"{session.Id}:{rawRefreshToken}";

        _logger.LogInformation("User logged in: {UserId} ({Role})", user.Id, user.Role);
        var authUserDto = BuildAuthUserDto(user);
        return new AuthResponse(accessToken, refreshTokenPayload, authUserDto);
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        if (!TryParsePayload(request.RefreshToken, out var sessionId, out var rawToken))
            throw new ValidationException("Invalid refresh token format");

        var session = await _unitOfWork.Sessions.GetByIdAsync(sessionId);
        if (session == null || session.IsRevoked || session.ExpiresAt <= DateTime.UtcNow)
            throw new ValidationException("Invalid or expired refresh token");

        if (!BCrypt.Net.BCrypt.Verify(rawToken, session.RefreshTokenHash))
            throw new ValidationException("Invalid refresh token");

        var user = await _unitOfWork.Users.GetByIdAsync(session.UserId);
        if (user == null || !user.IsActive)
            throw new ForbiddenException("User inactive or not found");

        var newAccessToken = _jwtService.GenerateAccessToken(user);
        var newRawRefreshToken = _jwtService.GenerateRefreshToken();

        session.IsRevoked = true;

        var newSession = new Session
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            RefreshTokenHash = BCrypt.Net.BCrypt.HashPassword(newRawRefreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        await _unitOfWork.Sessions.AddAsync(newSession);
        await _unitOfWork.CompleteAsync();

        var newRefreshTokenPayload = $"{newSession.Id}:{newRawRefreshToken}";
        return new AuthResponse(newAccessToken, newRefreshTokenPayload, BuildAuthUserDto(user));
    }

    public async Task VerifyEmailAsync(string token)
    {
        if (!TryParsePayload(token, out var tokenId, out var rawToken))
            throw new ValidationException("Invalid or expired token");

        var userToken = await _unitOfWork.UserTokens.GetByIdAsync(tokenId);
        if (userToken == null || userToken.IsUsed || userToken.ExpiresAt <= DateTime.UtcNow)
            throw new ValidationException("Invalid or expired token");

        if (userToken.TokenType != TokenType.EmailVerification)
            throw new ValidationException("Invalid token type");

        if (!BCrypt.Net.BCrypt.Verify(rawToken, userToken.TokenHash))
            throw new ValidationException("Invalid or expired token");

        userToken.IsUsed = true;
        var user = await _unitOfWork.Users.GetByIdAsync(userToken.UserId);
        if (user != null)
            user.IsEmailVerified = true;

        await _unitOfWork.CompleteAsync();
    }

    public async Task LogoutAsync(string userId)
    {
        var uid = Guid.Parse(userId);
        var activeSessions = await _unitOfWork.Sessions.FindAsync(s => s.UserId == uid && !s.IsRevoked);
        foreach (var session in activeSessions)
            session.IsRevoked = true;
        await _unitOfWork.CompleteAsync();
        _logger.LogInformation("User logged out: {UserId}", userId);
    }

    public async Task ForgotPasswordAsync(string email)
    {
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return; // silent success to prevent email enumeration

        var rawToken = _jwtService.GenerateRefreshToken();
        var userToken = new UserToken
        {
            Id = Guid.NewGuid(),
            User = user,
            TokenType = TokenType.PasswordReset,
            TokenHash = BCrypt.Net.BCrypt.HashPassword(rawToken),
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };
        await _unitOfWork.UserTokens.AddAsync(userToken);
        await _unitOfWork.CompleteAsync();

        var resetPayload = $"{userToken.Id}:{rawToken}";
        await _emailService.SendPasswordResetAsync(user.Email, resetPayload);
    }

    public async Task ResetPasswordCustomerAsync(ResetPasswordRequest request)
    {
        if (!TryParsePayload(request.Token, out var tokenId, out var rawToken))
            throw new ValidationException("Invalid or expired token");

        var userToken = await _unitOfWork.UserTokens.GetByIdAsync(tokenId);
        if (userToken == null || userToken.IsUsed || userToken.ExpiresAt <= DateTime.UtcNow)
            throw new ValidationException("Invalid or expired token");

        if (userToken.TokenType != TokenType.PasswordReset)
            throw new ValidationException("Invalid token type");

        if (!BCrypt.Net.BCrypt.Verify(rawToken, userToken.TokenHash))
            throw new ValidationException("Invalid or expired token");

        userToken.IsUsed = true;
        var user = await _unitOfWork.Users.GetByIdAsync(userToken.UserId);
        if (user != null)
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

        await _unitOfWork.CompleteAsync();
    }

    public async Task ResetPasswordAsync(string targetUserId, string newPassword, string adminId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(Guid.Parse(targetUserId));
        if (user == null) throw new NotFoundException("User not found");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _unitOfWork.CompleteAsync();
    }

    public async Task<RegistrationResponse> RegisterAgentAsync(RegisterAgentRequest request, string adminId)
    {
        var existingUser = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (existingUser != null)
            throw new ConflictException("Email already registered");

        var salutation = Enum.TryParse<Salutation>(request.Salutation, ignoreCase: true, out var sal) ? sal : Salutation.Mr;

        var user = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = UserRole.Agent,
            IsEmailVerified = true,
            IsActive = true,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            Salutation = salutation
        };

        var agent = new Agent
        {
            User = user,
            LicenseNumber = request.LicenseNumber,
            AgentType = AgentType.External,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.Agents.AddAsync(agent);
        await _unitOfWork.AuditLogs.AddAsync(new AuditLog
        {
            Id = Guid.NewGuid(), UserId = Guid.TryParse(adminId, out var aid) ? aid : Guid.Empty,
            EntityType = "User", EntityId = user.Id,
            Action = "AgentRegistered", NewValue = user.Email, CreatedAt = DateTime.UtcNow
        });
        await _unitOfWork.CompleteAsync();

        return new RegistrationResponse(user.Email, user.Role.ToString());
    }

    // --- helpers ---

    private static bool TryParsePayload(string payload, out Guid id, out string rawToken)
    {
        id = Guid.Empty;
        rawToken = string.Empty;
        var separatorIndex = payload.IndexOf(':');
        if (separatorIndex <= 0) return false;
        if (!Guid.TryParse(payload[..separatorIndex], out id)) return false;
        rawToken = payload[(separatorIndex + 1)..];
        return !string.IsNullOrEmpty(rawToken);
    }

    private static AuthUserDto BuildAuthUserDto(User user) =>
        new AuthUserDto(
            user.Id,
            user.Email,
            user.Salutation.ToString(),
            user.FirstName,
            user.LastName,
            $"{user.FirstName} {user.LastName}",
            user.Phone,
            user.Role.ToString(),
            user.Customer?.MaritalStatus.ToString() ?? "Single"
        );
}
