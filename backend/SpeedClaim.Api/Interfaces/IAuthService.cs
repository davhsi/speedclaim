using System.Threading.Tasks;
using SpeedClaim.Api.Dtos.Auth;

namespace SpeedClaim.Api.Interfaces;

public interface IAuthService
{
    Task<RegistrationResponse> RegisterCustomerAsync(RegisterUserRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task VerifyEmailAsync(string token);
    Task LogoutAsync(string userId);
    Task ForgotPasswordAsync(string email);
    Task ResetPasswordCustomerAsync(ResetPasswordRequest request);
    Task ResetPasswordAsync(string targetUserId, string newPassword, string adminId);
    Task<RegistrationResponse> RegisterAgentAsync(RegisterAgentRequest request, string adminId);
}
