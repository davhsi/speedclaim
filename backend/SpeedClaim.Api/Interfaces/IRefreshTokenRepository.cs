using System.Threading.Tasks;
using SpeedClaim.Api.Models;

namespace SpeedClaim.Api.Interfaces;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> GetByTokenWithUserAsync(string token);
}
