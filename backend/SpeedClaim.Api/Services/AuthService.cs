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
    private readonly SpeedClaimDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IConfiguration _configuration;
    private readonly IMapper _mapper;

    public AuthService(SpeedClaimDbContext context, IJwtService jwtService, IConfiguration configuration, IMapper mapper)
    {
        _context = context;
        _jwtService = jwtService;
        _configuration = configuration;
        _mapper = mapper;
    }

    public async Task<AuthResponse> RegisterUserAsync(RegisterUserRequest request)
    {
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            throw new ArgumentException("Email is already registered.");
        }

        var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Code == "Customer")
            ?? throw new InvalidOperationException("Customer role not found in database.");

        var user = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName,
            Phone = request.Phone,
            Address = request.Address,
            ProfilePictureUrl = "",
            IsActive = true
        };

        user.UserRoles.Add(new UserRole
        {
            Role = customerRole,
            Domain = "ALL"
        });

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return await GenerateAuthResponseAsync(user);
    }

    public async Task<AuthResponse> RegisterAgentAsync(RegisterAgentRequest request)
    {
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            throw new ArgumentException("Email is already registered.");
        }

        var agentRole = await _context.Roles.FirstOrDefaultAsync(r => r.Code == "Agent")
            ?? throw new InvalidOperationException("Agent role not found in database.");

        var user = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName,
            Phone = request.Phone,
            Address = request.Address,
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

        _context.Users.Add(user);
        _context.Agents.Add(agent);
        await _context.SaveChangesAsync();

        return await GenerateAuthResponseAsync(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == request.Email);

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
        var storedToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .ThenInclude(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(rt => rt.TokenHash == request.RefreshToken);

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
        _context.RefreshTokens.Update(storedToken);

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

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        var userDto = _mapper.Map<UserDto>(user);

        return new AuthResponse(accessToken, refreshTokenStr, userDto);
    }
}
