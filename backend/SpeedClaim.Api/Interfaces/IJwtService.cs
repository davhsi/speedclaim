using SpeedClaim.Api.Models;

namespace SpeedClaim.Api.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(User user, Guid sessionId);
    string GenerateRefreshToken();
}
