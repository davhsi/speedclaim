using System.Threading.Tasks;
using SpeedClaim.Core.Dtos.Auth;

namespace SpeedClaim.Core.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, string? ipAddress);
    Task<LoginResponse> RegisterAsync(RegisterRequest request, string? ipAddress);
    Task<LoginResponse> RefreshTokenAsync(string refreshToken, string? ipAddress);
    Task RevokeTokenAsync(string refreshToken, string? ipAddress);
}
