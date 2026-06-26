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

public class ProposalRepository : Repository<Proposal>
{
    public ProposalRepository(SpeedClaimDbContext context) : base(context) { }

    public override async Task<Proposal?> GetByIdAsync(Guid id)
    {
        return await Context.Proposals
            .Include(p => p.Product)
            .SingleOrDefaultAsync(p => p.Id == id);
    }

    public override async Task<IEnumerable<Proposal>> FindAsync(Expression<Func<Proposal, bool>> predicate)
    {
        return await Context.Proposals
            .Include(p => p.Product)
            .Where(predicate)
            .ToListAsync();
    }

    public override async Task<IEnumerable<Proposal>> GetAllAsync()
    {
        return await Context.Proposals
            .Include(p => p.Product)
            .ToListAsync();
    }

    public override async Task<(IEnumerable<Proposal> Items, int TotalCount)> GetPagedAsync(
        int pageNumber, int pageSize,
        Expression<Func<Proposal, bool>>? predicate = null,
        Func<IQueryable<Proposal>, IQueryable<Proposal>>? include = null)
    {
        IQueryable<Proposal> query = Context.Proposals.Include(p => p.Product);
        if (predicate != null) query = query.Where(predicate);
        var totalCount = await query.CountAsync();
        var items = await query.OrderByDescending(p => p.CreatedAt)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, totalCount);
    }
}
