using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SpeedClaim.Api.Dtos.Auth;
using SpeedClaim.Api.Exceptions;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;
using SpeedClaim.Api.Models.Enums;

namespace SpeedClaim.Api.Services;

public class ExternalIdentityService : IExternalIdentityService
{
    public const string Auth0Provider = "Auth0";
    private static readonly TimeSpan LinkCodeLifetime = TimeSpan.FromMinutes(10);

    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ExternalIdentityService> _logger;

    public ExternalIdentityService(IUnitOfWork unitOfWork, ILogger<ExternalIdentityService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ExternalIdentityLinkCodeResponse> CreateAuth0LinkCodeAsync(Guid userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null || !user.IsActive || !user.IsEmailVerified || user.Role != UserRole.Customer)
            throw new ForbiddenException("Only an active, verified customer can link an external identity.");

        var rawCode = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var expiresAt = DateTime.UtcNow.Add(LinkCodeLifetime);
        var linkToken = new UserToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenType = TokenType.ExternalIdentityLink,
            TokenHash = BCrypt.Net.BCrypt.HashPassword(rawCode),
            ExpiresAt = expiresAt
        };

        await _unitOfWork.UserTokens.AddAsync(linkToken);
        await _unitOfWork.AuditLogs.AddAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EntityType = "ExternalIdentity",
            EntityId = linkToken.Id,
            Action = "ExternalIdentityLinkCodeCreated",
            NewValue = JsonSerializer.Serialize(new { provider = Auth0Provider, expiresAt }),
            CreatedAt = DateTime.UtcNow
        });
        _unitOfWork.SetCurrentActor(userId);
        await _unitOfWork.CompleteAsync();

        return new ExternalIdentityLinkCodeResponse(Auth0Provider, $"{linkToken.Id}:{rawCode}", expiresAt);
    }

    public async Task LinkAuth0SubjectAsync(string linkCode, string subject)
    {
        if (!TryParseLinkCode(linkCode, out var tokenId, out var rawCode))
            throw new ValidationException("Invalid external identity link code.");
        if (string.IsNullOrWhiteSpace(subject) || subject.Length > 255)
            throw new ValidationException("Invalid external identity subject.");

        var linkToken = await _unitOfWork.UserTokens.GetByIdAsync(tokenId);
        if (linkToken == null || linkToken.TokenType != TokenType.ExternalIdentityLink || linkToken.IsUsed || linkToken.ExpiresAt <= DateTime.UtcNow ||
            !BCrypt.Net.BCrypt.Verify(rawCode, linkToken.TokenHash))
            throw new ValidationException("Invalid or expired external identity link code.");

        var user = await _unitOfWork.Users.GetByIdAsync(linkToken.UserId);
        if (user == null || !user.IsActive || !user.IsEmailVerified || user.Role != UserRole.Customer)
            throw new ForbiddenException("The SpeedClaim account is not eligible for external identity linking.");

        var subjectLink = await _unitOfWork.ExternalIdentities
            .FirstOrDefaultAsync(identity => identity.Provider == Auth0Provider && identity.Subject == subject);
        if (subjectLink != null && subjectLink.UserId != user.Id)
            throw new ConflictException("This Auth0 identity is already linked to another SpeedClaim account.");

        var userLink = await _unitOfWork.ExternalIdentities
            .FirstOrDefaultAsync(identity => identity.Provider == Auth0Provider && identity.UserId == user.Id);
        if (userLink != null && userLink.Subject != subject)
            throw new ConflictException("This SpeedClaim account is already linked to a different Auth0 identity.");

        linkToken.IsUsed = true;
        if (subjectLink == null)
        {
            subjectLink = new ExternalIdentity
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Provider = Auth0Provider,
                Subject = subject,
                LinkedAt = DateTimeOffset.UtcNow,
                LastAuthenticatedAt = DateTimeOffset.UtcNow
            };
            await _unitOfWork.ExternalIdentities.AddAsync(subjectLink);
        }
        else
        {
            subjectLink.LastAuthenticatedAt = DateTimeOffset.UtcNow;
            _unitOfWork.ExternalIdentities.Update(subjectLink);
        }

        await _unitOfWork.AuditLogs.AddAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            EntityType = "ExternalIdentity",
            EntityId = subjectLink.Id,
            Action = "ExternalIdentityLinked",
            NewValue = JsonSerializer.Serialize(new { provider = Auth0Provider }),
            CreatedAt = DateTime.UtcNow
        });
        _unitOfWork.SetCurrentActor(user.Id);
        await _unitOfWork.CompleteAsync();
        _logger.LogInformation("Linked Auth0 identity to SpeedClaim user {UserId}", user.Id);
    }

    public async Task<Guid?> ResolveActiveUserIdAsync(string provider, string subject)
    {
        if (!string.Equals(provider, Auth0Provider, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(subject))
            return null;

        var identity = await _unitOfWork.ExternalIdentities
            .FirstOrDefaultAsync(item => item.Provider == Auth0Provider && item.Subject == subject);
        if (identity == null)
            return null;

        var user = await _unitOfWork.Users.GetByIdAsync(identity.UserId);
        if (user == null || !user.IsActive || !user.IsEmailVerified || user.Role != UserRole.Customer)
            return null;

        identity.LastAuthenticatedAt = DateTimeOffset.UtcNow;
        _unitOfWork.ExternalIdentities.Update(identity);
        _unitOfWork.SetCurrentActor(user.Id);
        await _unitOfWork.CompleteAsync();
        return user.Id;
    }

    public async Task<IReadOnlyList<LinkedExternalIdentityDto>> ListAsync(Guid userId)
    {
        var identities = await _unitOfWork.ExternalIdentities.FindAsync(identity => identity.UserId == userId);
        return identities
            .OrderBy(identity => identity.Provider)
            .Select(identity => new LinkedExternalIdentityDto(identity.Provider, identity.LinkedAt))
            .ToList();
    }

    private static bool TryParseLinkCode(string linkCode, out Guid tokenId, out string rawCode)
    {
        tokenId = Guid.Empty;
        rawCode = string.Empty;
        var parts = linkCode.Split(':', 2, StringSplitOptions.TrimEntries);
        return parts.Length == 2 && Guid.TryParse(parts[0], out tokenId) && parts[1].Length == 64 && (rawCode = parts[1]).All(Uri.IsHexDigit);
    }
}
