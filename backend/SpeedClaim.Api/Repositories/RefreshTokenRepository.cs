using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SpeedClaim.Api.Context;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;

namespace SpeedClaim.Api.Repositories;

public class RefreshTokenRepository : Repository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(SpeedClaimDbContext context) : base(context)
    {
    }

    public async Task<RefreshToken?> GetByTokenWithUserAsync(string token)
    {
        return await Context.RefreshTokens
            .Include(rt => rt.User)
            .ThenInclude(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .SingleOrDefaultAsync(rt => rt.TokenHash == token);
    }
}
