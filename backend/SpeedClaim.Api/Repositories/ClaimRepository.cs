using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SpeedClaim.Api.Context;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;

namespace SpeedClaim.Api.Repositories;

public class ClaimRepository : Repository<Claim>, IClaimRepository
{
    public ClaimRepository(SpeedClaimDbContext context) : base(context)
    {
    }

    public async Task<Claim?> GetClaimWithDetailsAsync(Guid claimId)
    {
        return await Context.Claims
            .Include(c => c.Workflows.OrderByDescending(w => w.TransitionedAt))
            .Include(c => c.Documents)
            .SingleOrDefaultAsync(c => c.Id == claimId);
    }
}
