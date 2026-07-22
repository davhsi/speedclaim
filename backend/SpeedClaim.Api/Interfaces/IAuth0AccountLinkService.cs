using SpeedClaim.Api.Dtos.Auth;

namespace SpeedClaim.Api.Interfaces;

/// <summary>
/// Creates and completes a customer-initiated Auth0 Authorization Code + PKCE transaction.
/// The external AI host never receives an account-linking secret.
/// </summary>
public interface IAuth0AccountLinkService
{
    Task<ExternalIdentityAuthorizationResponse> StartAsync(Guid userId);
    Task<string> CompleteAsync(string? code, string? state, string? error);
}
