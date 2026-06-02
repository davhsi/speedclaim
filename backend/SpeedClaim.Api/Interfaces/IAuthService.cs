using SpeedClaim.Api.Dtos.Auth;

namespace SpeedClaim.Api.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterUserAsync(RegisterUserRequest request);
    Task<AuthResponse> RegisterAgentAsync(RegisterAgentRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);
}
