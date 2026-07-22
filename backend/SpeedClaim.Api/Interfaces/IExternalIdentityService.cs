using SpeedClaim.Api.Dtos.Auth;

namespace SpeedClaim.Api.Interfaces;

/// <summary>
/// Owns the durable link between an external OIDC subject and a SpeedClaim user.
/// A customer-initiated OAuth transaction supplies the verified subject.
/// </summary>
public interface IExternalIdentityService
{
    Task LinkAuth0SubjectAsync(Guid userId, string subject);
    /// <summary>
    /// Creates the durable Auth0 subject mapping only when an Auth0-verified email exactly
    /// identifies one active, verified SpeedClaim customer. Returns null for every unsafe or
    /// ineligible match rather than guessing an account.
    /// </summary>
    Task<Guid?> TryAutoLinkAuth0SubjectByVerifiedEmailAsync(string subject, string verifiedEmail);
    Task<Guid?> ResolveActiveUserIdAsync(string provider, string subject);
    Task<IReadOnlyList<LinkedExternalIdentityDto>> ListAsync(Guid userId);
}
