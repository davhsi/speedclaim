using System;
using System.Text.Json;
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

        var existingPhone = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Phone == request.Phone);
        if (existingPhone != null)
            throw new ConflictException("Phone number already registered");

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
            Gender = request.Gender,
            MaritalStatus = request.MaritalStatus
        };

        var ip = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.Customers.AddAsync(customer);

        // Create permanent address
        await _unitOfWork.Addresses.AddAsync(new Address
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            AddressType = AddressType.Permanent,
            AddressLine1 = request.PermanentAddress.Line1,
            AddressLine2 = request.PermanentAddress.Line2,
            City = request.PermanentAddress.City,
            State = request.PermanentAddress.State,
            Pincode = request.PermanentAddress.PostalCode,
            Country = request.PermanentAddress.Country,
            IsSameAsPermanent = false,
            CreatedAt = DateTimeOffset.UtcNow
        });

        // Create current address if different
        if (!request.IsSameAsPermanent && request.CurrentAddress != null)
        {
            await _unitOfWork.Addresses.AddAsync(new Address
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                AddressType = AddressType.Current,
                AddressLine1 = request.CurrentAddress.Line1,
                AddressLine2 = request.CurrentAddress.Line2,
                City = request.CurrentAddress.City,
                State = request.CurrentAddress.State,
                Pincode = request.CurrentAddress.PostalCode,
                Country = request.CurrentAddress.Country,
                IsSameAsPermanent = request.IsSameAsPermanent,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }
        else if (request.IsSameAsPermanent)
        {
            // Mark permanent address as also being current
            var permanentAddr = new Address
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                AddressType = AddressType.Current,
                AddressLine1 = request.PermanentAddress.Line1,
                AddressLine2 = request.PermanentAddress.Line2,
                City = request.PermanentAddress.City,
                State = request.PermanentAddress.State,
                Pincode = request.PermanentAddress.PostalCode,
                Country = request.PermanentAddress.Country,
                IsSameAsPermanent = true,
                CreatedAt = DateTimeOffset.UtcNow
            };
            await _unitOfWork.Addresses.AddAsync(permanentAddr);
        }

        await _unitOfWork.AuditLogs.AddAsync(new AuditLog
        {
            Id = Guid.NewGuid(), UserId = user.Id, EntityType = "User", EntityId = user.Id,
            Action = "CustomerRegistered", NewValue = JsonSerializer.Serialize(user.Email), IpAddress = ip, CreatedAt = DateTime.UtcNow
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

        if (user == null)
        {
            _logger.LogWarning("Login attempt for non-existent email: {Email}", request.Email);
            throw new NotFoundException("No account found with this email address.");
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= maxAttempts)
            {
                user.LockedUntil = DateTime.UtcNow.AddMinutes(lockoutMinutes);
                _logger.LogWarning("Account locked after {Max} failed attempts: {Email}", maxAttempts, request.Email);
            }
            await _unitOfWork.CompleteAsync();
            _logger.LogWarning("Failed login attempt for email: {Email}", request.Email);
            throw new ValidationException("Incorrect password.");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login attempt for inactive account: {Email}", request.Email);
            throw new ForbiddenException("Account is inactive");
        }

        if (!user.IsEmailVerified)
        {
            _logger.LogWarning("Login attempt for unverified email: {Email}", request.Email);
            throw new UnprocessableException("Please verify your email address before signing in.");
        }

        // Successful login — reset lockout state
        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;

        var rawRefreshToken = _jwtService.GenerateRefreshToken();

        var session = new Session
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            RefreshTokenHash = BCrypt.Net.BCrypt.HashPassword(rawRefreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IpAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
            UserAgent = _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString() ?? "Unknown"
        };

        var accessToken = _jwtService.GenerateAccessToken(user, session.Id);

        await _unitOfWork.Sessions.AddAsync(session);
        user.LastLoginAt = DateTime.UtcNow;
        await _unitOfWork.AuditLogs.AddAsync(new AuditLog
        {
            Id = Guid.NewGuid(), UserId = user.Id, EntityType = "Session", EntityId = session.Id,
            Action = "UserLoggedIn", NewValue = JsonSerializer.Serialize(user.Role.ToString()), CreatedAt = DateTime.UtcNow
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

        var newRawRefreshToken = _jwtService.GenerateRefreshToken();

        session.IsRevoked = true;

        var newSession = new Session
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            RefreshTokenHash = BCrypt.Net.BCrypt.HashPassword(newRawRefreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        var newAccessToken = _jwtService.GenerateAccessToken(user, newSession.Id);

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
        if (userToken == null || userToken.ExpiresAt <= DateTime.UtcNow)
            throw new ValidationException("Invalid or expired token");

        // Idempotent: clicking the link a second time after success should not show an error
        if (userToken.IsUsed)
        {
            var alreadyVerified = await _unitOfWork.Users.GetByIdAsync(userToken.UserId);
            if (alreadyVerified?.IsEmailVerified == true)
                return;
            throw new ValidationException("Invalid or expired token");
        }

        if (userToken.TokenType != TokenType.EmailVerification)
            throw new ValidationException("Invalid token type");

        if (!BCrypt.Net.BCrypt.Verify(rawToken, userToken.TokenHash))
            throw new ValidationException("Invalid or expired token");

        userToken.IsUsed = true;
        var user = await _unitOfWork.Users.GetByIdAsync(userToken.UserId);
        if (user != null)
        {
            user.IsEmailVerified = true;
            await _unitOfWork.AuditLogs.AddAsync(new AuditLog
            {
                Id = Guid.NewGuid(), UserId = user.Id, EntityType = "User", EntityId = user.Id,
                Action = "EmailVerified", CreatedAt = DateTime.UtcNow
            });
        }

        await _unitOfWork.CompleteAsync();
    }

    public async Task ResendVerificationEmailAsync(string email)
    {
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null || user.IsEmailVerified)
            return;

        var existingTokens = await _unitOfWork.UserTokens.FindAsync(
            t => t.UserId == user.Id && t.TokenType == TokenType.EmailVerification && !t.IsUsed);
        foreach (var t in existingTokens)
            t.IsUsed = true;

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

        var verificationPayload = $"{userToken.Id}:{rawToken}";
        await _emailService.SendEmailVerificationAsync(user.Email, verificationPayload);

        _logger.LogInformation("Resent verification email to {Email}", email);
    }

    public async Task LogoutAsync(string userId)
    {
        var uid = Guid.Parse(userId);
        var activeSessions = await _unitOfWork.Sessions.FindAsync(s => s.UserId == uid && !s.IsRevoked);
        foreach (var session in activeSessions)
            session.IsRevoked = true;
        await _unitOfWork.AuditLogs.AddAsync(new AuditLog
        {
            Id = Guid.NewGuid(), UserId = uid, EntityType = "User", EntityId = uid,
            Action = "UserLoggedOut", CreatedAt = DateTime.UtcNow
        });
        await _unitOfWork.CompleteAsync();
        _logger.LogInformation("User logged out: {UserId}", userId);
    }

    public async Task ForgotPasswordAsync(string email)
    {
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return; // silent success to prevent email enumeration

        var existingTokens = await _unitOfWork.UserTokens.FindAsync(
            t => t.UserId == user.Id && t.TokenType == TokenType.PasswordReset && !t.IsUsed);
        foreach (var token in existingTokens ?? Array.Empty<UserToken>())
            token.IsUsed = true;

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
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.FailedLoginAttempts = 0;
            user.LockedUntil = null;
            await RevokeActiveSessionsAsync(user.Id);
            await _unitOfWork.AuditLogs.AddAsync(new AuditLog
            {
                Id = Guid.NewGuid(), UserId = user.Id, EntityType = "User", EntityId = user.Id,
                Action = "PasswordReset", CreatedAt = DateTime.UtcNow
            });
        }

        await _unitOfWork.CompleteAsync();
    }

    public async Task ResetPasswordAsync(string targetUserId, string newPassword, string adminId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(Guid.Parse(targetUserId));
        if (user == null) throw new NotFoundException("User not found");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;
        await RevokeActiveSessionsAsync(user.Id);
        await _unitOfWork.AuditLogs.AddAsync(new AuditLog
        {
            Id = Guid.NewGuid(), UserId = Guid.Parse(adminId), EntityType = "User", EntityId = user.Id,
            Action = "PasswordResetByAdmin",
            NewValue = JsonSerializer.Serialize(new { targetEmail = user.Email }),
            CreatedAt = DateTime.UtcNow
        });
        await _unitOfWork.CompleteAsync();
    }

    private async Task RevokeActiveSessionsAsync(Guid userId)
    {
        var activeSessions = await _unitOfWork.Sessions.FindAsync(s => s.UserId == userId && !s.IsRevoked);
        foreach (var session in activeSessions ?? Array.Empty<Session>())
        {
            session.IsRevoked = true;
            session.UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    public async Task<RegistrationResponse> RegisterAgentAsync(RegisterAgentRequest request, string adminId)
    {
        var existingUser = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (existingUser != null)
            throw new ConflictException("Email already registered");

        var existingPhone = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Phone == request.Phone);
        if (existingPhone != null)
            throw new ConflictException("Phone number already registered");

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
            LicenseExpiry = request.LicenseExpiry,
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
            Action = "AgentRegistered", NewValue = JsonSerializer.Serialize(user.Email), CreatedAt = DateTime.UtcNow
        });
        await _unitOfWork.CompleteAsync();

        await _emailService.SendTemplatedEmailAsync("AgentWelcome", new System.Collections.Generic.Dictionary<string, string>
        {
            ["firstName"] = System.Net.WebUtility.HtmlEncode(user.FirstName),
            ["email"]     = System.Net.WebUtility.HtmlEncode(user.Email)
        }, user.Email);

        return new RegistrationResponse(user.Email, user.Role.ToString());
    }

    public async Task<RegistrationResponse> InviteStaffUserAsync(AdminInviteUserRequest request, string adminId)
    {
        var existing = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (existing != null)
            throw new ConflictException("Email already registered");

        if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role)
            || role == UserRole.Customer || role == UserRole.Agent)
            throw new ValidationException("Only staff roles can be invited: Underwriter, ClaimsOfficer, FinanceOfficer, Surveyor, or Admin.");

        var user = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
            Role = role,
            IsEmailVerified = true,
            IsActive = true,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = "0000000000",
            Salutation = Salutation.Mr
        };

        await _unitOfWork.Users.AddAsync(user);

        if (role == UserRole.Surveyor)
        {
            await _unitOfWork.Surveyors.AddAsync(new Models.Surveyor
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                SurveyorType = Models.Enums.SurveyorType.Internal,
                Specialization = Models.Enums.SurveyorSpecialization.All,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        await _unitOfWork.AuditLogs.AddAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = Guid.TryParse(adminId, out var aid) ? aid : Guid.Empty,
            EntityType = "User",
            EntityId = user.Id,
            Action = "StaffInvited",
            NewValue = JsonSerializer.Serialize(new { user.Email, Role = user.Role.ToString() }),
            CreatedAt = DateTime.UtcNow
        });
        await _unitOfWork.CompleteAsync();

        // Send a password-reset link so the invited user sets their own password on first login
        await ForgotPasswordAsync(request.Email);

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
            user.Customer?.MaritalStatus.ToString() ?? "Single",
            user.AvatarUrl != null ? $"/{user.AvatarUrl}" : null
        );
}
