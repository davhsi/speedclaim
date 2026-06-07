using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using AutoMapper;
using SpeedClaim.Api.Context;
using SpeedClaim.Api.Dtos.Auth;
using SpeedClaim.Api.Models;
using SpeedClaim.Api.Interfaces;
using BCrypt.Net;

namespace SpeedClaim.Api.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtService _jwtService;
    private readonly IConfiguration _configuration;
    private readonly IMapper _mapper;
    private readonly IEmailService _emailService;

    public AuthService(IUnitOfWork unitOfWork, IJwtService jwtService, IConfiguration configuration, IMapper mapper, IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
        _configuration = configuration;
        _mapper = mapper;
        _emailService = emailService;
    }

    public async Task<AuthResponse> RegisterUserAsync(RegisterUserRequest request)
    {
        if (await _unitOfWork.Users.AnyAsync(u => u.Email == request.Email))
        {
            throw new ArgumentException("Email is already registered.");
        }
        
        if (!string.IsNullOrEmpty(request.AadhaarNumber) && await _unitOfWork.Users.AnyAsync(u => u.AadhaarNumber == request.AadhaarNumber))
        {
            throw new ArgumentException("Aadhaar Number is already registered to another user.");
        }
        
        if (!string.IsNullOrEmpty(request.PanNumber) && await _unitOfWork.Users.AnyAsync(u => u.PanNumber == request.PanNumber))
        {
            throw new ArgumentException("PAN Number is already registered to another user.");
        }

        var customerRole = await _unitOfWork.Roles.FirstOrDefaultAsync(r => r.Code == "Customer")
            ?? throw new InvalidOperationException("Customer role not found in database.");

        var user = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Salutation = request.Salutation,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            Address = new Address
            {
                Street = request.Address.Street,
                City = request.Address.City,
                State = request.Address.State,
                PostalCode = request.Address.PostalCode,
                Country = request.Address.Country
            },
            DateOfBirth = request.DateOfBirth,
            AadhaarNumber = request.AadhaarNumber,
            PanNumber = request.PanNumber,
            Gender = request.Gender,
            MaritalStatus = request.MaritalStatus,
            ProfilePictureUrl = "",
            IsActive = true
        };

        user.UserRoles.Add(new UserRole
        {
            Role = customerRole,
            Domain = "ALL"
        });

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.CompleteAsync();

        var emailSubject = "Welcome to SpeedClaim!";
        var emailBody = $"<h1>Welcome {user.FullName}!</h1><p>Your account has been created successfully. You can now login to manage your insurance policies and claims.</p>";
        await _emailService.SendEmailAsync(user.Email, user.FullName, emailSubject, emailBody);

        return await GenerateAuthResponseAsync(user);
    }

    public async Task<AuthResponse> RegisterAgentAsync(RegisterAgentRequest request)
    {
        if (await _unitOfWork.Users.AnyAsync(u => u.Email == request.Email))
        {
            throw new ArgumentException("Email is already registered.");
        }
        
        if (!string.IsNullOrEmpty(request.AadhaarNumber) && await _unitOfWork.Users.AnyAsync(u => u.AadhaarNumber == request.AadhaarNumber))
        {
            throw new ArgumentException("Aadhaar Number is already registered to another user.");
        }
        
        if (!string.IsNullOrEmpty(request.PanNumber) && await _unitOfWork.Users.AnyAsync(u => u.PanNumber == request.PanNumber))
        {
            throw new ArgumentException("PAN Number is already registered to another user.");
        }

        var agentRole = await _unitOfWork.Roles.FirstOrDefaultAsync(r => r.Code == "Agent")
            ?? throw new InvalidOperationException("Agent role not found in database.");

        var user = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Salutation = request.Salutation,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            Address = new Address
            {
                Street = request.Address.Street,
                City = request.Address.City,
                State = request.Address.State,
                PostalCode = request.Address.PostalCode,
                Country = request.Address.Country
            },
            AadhaarNumber = request.AadhaarNumber,
            PanNumber = request.PanNumber,
            MaritalStatus = request.MaritalStatus,
            ProfilePictureUrl = "",
            IsActive = true
        };

        user.UserRoles.Add(new UserRole
        {
            Role = agentRole,
            Domain = "ALL"
        });

        var agent = new Agent
        {
            LicenseNumber = request.LicenseNumber,
            AgencyName = request.AgencyName,
            LicenseValidUntil = DateTime.UtcNow.AddYears(1), // Default 1 year from now
            IsActive = false, // Agent needs to be verified/approved
            User = user
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.Agents.AddAsync(agent);
        await _unitOfWork.CompleteAsync();

        return await GenerateAuthResponseAsync(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _unitOfWork.Users.GetUserByEmailWithRolesAsync(request.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Your account has been deactivated.");
        }

        return await GenerateAuthResponseAsync(user);
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var storedToken = await _unitOfWork.RefreshTokens
            .GetByTokenWithUserAsync(request.RefreshToken);

        if (storedToken == null || storedToken.ExpiresAt < DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");
        }

        if (storedToken.IsRevoked)
        {
            throw new UnauthorizedAccessException("Refresh token has been revoked.");
        }

        // Revoke current token
        storedToken.IsRevoked = true;
        _unitOfWork.RefreshTokens.Update(storedToken);
        await _unitOfWork.CompleteAsync();

        return await GenerateAuthResponseAsync(storedToken.User);
    }

    private async Task<AuthResponse> GenerateAuthResponseAsync(User user)
    {
        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshTokenStr = _jwtService.GenerateRefreshToken();
        
        var expirationDaysStr = _configuration["JwtSettings:RefreshTokenExpirationDays"] ?? "7";
        var expirationDays = int.Parse(expirationDaysStr);

        var refreshToken = new RefreshToken
        {
            TokenHash = refreshTokenStr,
            ExpiresAt = DateTime.UtcNow.AddDays(expirationDays),
            UserId = user.Id,
            IpAddress = "N/A" // To be passed from controller context ideally
        };

        await _unitOfWork.RefreshTokens.AddAsync(refreshToken);
        await _unitOfWork.CompleteAsync();

        var userDto = _mapper.Map<UserDto>(user);

        return new AuthResponse(accessToken, refreshTokenStr, userDto);
    }
}
