using SpeedClaim.Api.Dtos.Auth;

namespace SpeedClaim.Api.Interfaces;

/// <summary>
/// Owns the durable link between an external OIDC subject and a SpeedClaim user.
/// A customer-initiated OAuth transaction supplies the verified subject.
/// </summary>
public interface IExternalIdentityService
{
    Task LinkAuth0SubjectAsync(Guid userId, string subject);
    Task<Guid?> ResolveActiveUserIdAsync(string provider, string subject);
    Task<IReadOnlyList<LinkedExternalIdentityDto>> ListAsync(Guid userId);
}
