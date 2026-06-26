using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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

    public override async Task<Claim?> GetByIdAsync(Guid id)
    {
        return await Context.Claims
            .Include(c => c.Policy)
            .Include(c => c.Customer).ThenInclude(cu => cu.User)
            .SingleOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Claim?> GetClaimWithDetailsAsync(Guid claimId)
    {
        return await Context.Claims
            .Include(c => c.Policy)
            .Include(c => c.Customer).ThenInclude(cu => cu.User)
            .Include(c => c.StatusHistory.OrderByDescending(w => w.ChangedAt))
            .SingleOrDefaultAsync(c => c.Id == claimId);
    }

    public override async Task<IEnumerable<Claim>> FindAsync(Expression<Func<Claim, bool>> predicate)
    {
        return await Context.Claims
            .Include(c => c.Policy)
            .Include(c => c.Customer).ThenInclude(cu => cu.User)
            .Where(predicate)
            .ToListAsync();
    }

    public override async Task<(IEnumerable<Claim> Items, int TotalCount)> GetPagedAsync(
        int pageNumber, int pageSize,
        Expression<Func<Claim, bool>>? predicate = null,
        Func<IQueryable<Claim>, IQueryable<Claim>>? include = null)
    {
        IQueryable<Claim> query = Context.Claims
            .Include(c => c.Policy)
            .Include(c => c.Customer).ThenInclude(cu => cu.User);

        if (predicate != null)
            query = query.Where(predicate);

        var totalCount = await query.CountAsync();
        var items = await query.OrderByDescending(c => c.CreatedAt)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, totalCount);
    }
}
