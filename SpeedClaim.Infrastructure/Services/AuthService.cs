using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SpeedClaim.Core.Dtos.Auth;
using SpeedClaim.Core.Entities.Auth;
using SpeedClaim.Core.Interfaces;
using SpeedClaim.Core.Options;
using SpeedClaim.Infrastructure.Data;

namespace SpeedClaim.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly JwtOptions _jwtOptions;

    public AuthService(AppDbContext context, IOptions<JwtOptions> jwtOptions)
    {
        _context = context;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, string? ipAddress)
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
            throw new UnauthorizedAccessException("User account is inactive.");
        }

        var roles = user.UserRoles
            .Where(ur => ur.RevokedAt == null)
            .Select(ur => ur.Role.Code)
            .ToList();

        var jwtToken = GenerateJwtToken(user, roles);
        var refreshToken = GenerateRefreshToken(user.Id, ipAddress);

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return new LoginResponse
        {
            AccessToken = jwtToken,
            RefreshToken = refreshToken.TokenHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(_jwtOptions.ExpiryMinutes)
        };
    }

    public async Task<LoginResponse> RegisterAsync(RegisterRequest request, string? ipAddress)
    {
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            throw new ArgumentException("Email is already registered.");
        }

        var user = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName,
            Phone = request.Phone,
            Address = request.Address,
            DateOfBirth = request.DateOfBirth,
            IsActive = true,
            IsEmailVerified = false
        };

        _context.Users.Add(user);

        // Assign default role 'CUSTOMER'
        var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Code == "CUSTOMER");
        if (customerRole != null)
        {
            _context.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = customerRole.Id,
                AssignedBy = "SYSTEM"
            });
        }

        await _context.SaveChangesAsync();

        // Optionally, generate token immediately or force login
        var roles = customerRole != null ? new List<string> { customerRole.Code } : new List<string>();
        var jwtToken = GenerateJwtToken(user, roles);
        var refreshToken = GenerateRefreshToken(user.Id, ipAddress);

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return new LoginResponse
        {
            AccessToken = jwtToken,
            RefreshToken = refreshToken.TokenHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(_jwtOptions.ExpiryMinutes)
        };
    }

    public async Task<LoginResponse> RefreshTokenAsync(string token, string? ipAddress)
    {
        var refreshToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .ThenInclude(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(rt => rt.TokenHash == token);

        if (refreshToken == null || refreshToken.IsRevoked || refreshToken.ExpiresAt < DateTimeOffset.UtcNow)
        {
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");
        }

        if (!refreshToken.User.IsActive)
        {
            throw new UnauthorizedAccessException("User account is inactive.");
        }

        // Revoke the old refresh token (sliding expiration pattern)
        refreshToken.IsRevoked = true;

        var roles = refreshToken.User.UserRoles
            .Where(ur => ur.RevokedAt == null)
            .Select(ur => ur.Role.Code)
            .ToList();

        var newJwtToken = GenerateJwtToken(refreshToken.User, roles);
        var newRefreshToken = GenerateRefreshToken(refreshToken.UserId, ipAddress);

        _context.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync();

        return new LoginResponse
        {
            AccessToken = newJwtToken,
            RefreshToken = newRefreshToken.TokenHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(_jwtOptions.ExpiryMinutes)
        };
    }

    public async Task RevokeTokenAsync(string token, string? ipAddress)
    {
        var refreshToken = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == token);
        if (refreshToken == null || refreshToken.IsRevoked) return;

        refreshToken.IsRevoked = true;
        await _context.SaveChangesAsync();
    }

    private string GenerateJwtToken(User user, IEnumerable<string> roles)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiryMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private RefreshToken GenerateRefreshToken(Guid userId, string? ipAddress)
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        var token = Convert.ToBase64String(randomNumber);

        return new RefreshToken
        {
            UserId = userId,
            TokenHash = token,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(_jwtOptions.RefreshTokenExpiryDays),
            IpAddress = ipAddress
        };
    }
}
