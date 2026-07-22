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
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ExternalIdentityService> _logger;

    public ExternalIdentityService(IUnitOfWork unitOfWork, ILogger<ExternalIdentityService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task LinkAuth0SubjectAsync(Guid userId, string subject)
    {
        await LinkAuth0SubjectAsync(userId, subject, "customer_initiated");
    }

    public async Task<Guid?> TryAutoLinkAuth0SubjectByVerifiedEmailAsync(string subject, string verifiedEmail)
    {
        if (string.IsNullOrWhiteSpace(subject) || subject.Length > 255
            || string.IsNullOrWhiteSpace(verifiedEmail) || verifiedEmail.Length > 255)
            return null;

        // Email is accepted only after Auth0 has signed it into the access token. It is never
        // obtained from an MCP argument or an AI-host supplied header.
        var user = await _unitOfWork.Users.GetUserByEmailAsync(verifiedEmail.Trim());
        if (user == null || !user.IsActive || !user.IsEmailVerified || user.Role != UserRole.Customer)
            return null;

        try
        {
            await LinkAuth0SubjectAsync(user.Id, subject, "automatic_verified_email");
            return user.Id;
        }
        catch (ConflictException)
        {
            // A subject/customer conflict must never be resolved by choosing another account.
            return null;
        }
    }

    private async Task LinkAuth0SubjectAsync(Guid userId, string subject, string linkingMethod)
    {
        if (string.IsNullOrWhiteSpace(subject) || subject.Length > 255)
            throw new ValidationException("Invalid external identity subject.");

        var user = await _unitOfWork.Users.GetByIdAsync(userId);
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
            NewValue = JsonSerializer.Serialize(new { provider = Auth0Provider, linkingMethod }),
            CreatedAt = DateTime.UtcNow
        });
        _unitOfWork.SetCurrentActor(user.Id);
        await _unitOfWork.CompleteAsync();
        _logger.LogInformation("Linked Auth0 identity to SpeedClaim user {UserId} through {LinkingMethod}", user.Id, linkingMethod);
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

}
