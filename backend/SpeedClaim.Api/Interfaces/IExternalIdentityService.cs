using SpeedClaim.Api.Dtos.Auth;

namespace SpeedClaim.Api.Interfaces;

/// <summary>
/// Owns the durable link between an external OIDC subject and a SpeedClaim user.
/// An MCP/OAuth adapter consumes link codes; it must never choose a user id itself.
/// </summary>
public interface IExternalIdentityService
{
    Task<ExternalIdentityLinkCodeResponse> CreateAuth0LinkCodeAsync(Guid userId);
    Task LinkAuth0SubjectAsync(string linkCode, string subject);
    Task<Guid?> ResolveActiveUserIdAsync(string provider, string subject);
    Task<IReadOnlyList<LinkedExternalIdentityDto>> ListAsync(Guid userId);
}
