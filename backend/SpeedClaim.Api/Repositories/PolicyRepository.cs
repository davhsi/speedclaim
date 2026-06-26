using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SpeedClaim.Api.Context;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;
using SpeedClaim.Api.Models.Enums;

namespace SpeedClaim.Api.Repositories;

public class PolicyRepository : Repository<Policy>, IPolicyRepository
{
    public PolicyRepository(SpeedClaimDbContext context) : base(context)
    {
    }

    public async Task<Policy?> GetMotorPolicyByIdAsync(Guid id)
    {
        return await Context.Policies
            .Include(p => p.Product)
            .Include(p => p.MotorDetail)
            .SingleOrDefaultAsync(p => p.Id == id && p.MotorDetail != null);
    }

    public async Task<Policy?> GetHealthPolicyByIdAsync(Guid id)
    {
        return await Context.Policies
            .Include(p => p.Product)
            .Include(p => p.HealthDetail)
            .Include(p => p.PolicyMembers)
            .SingleOrDefaultAsync(p => p.Id == id && p.HealthDetail != null);
    }

    public async Task<Policy?> GetLifePolicyByIdAsync(Guid id)
    {
        return await Context.Policies
            .Include(p => p.Product)
            .Include(p => p.LifeDetail)
            .SingleOrDefaultAsync(p => p.Id == id && p.LifeDetail != null);
    }

    public override async Task<Policy?> GetByIdAsync(Guid id)
    {
        return await Context.Policies
            .Include(p => p.Product)
            .SingleOrDefaultAsync(p => p.Id == id);
    }

    public override async Task<IEnumerable<Policy>> FindAsync(Expression<Func<Policy, bool>> predicate)
    {
        return await Context.Policies
            .Include(p => p.Product)
            .Where(predicate)
            .ToListAsync();
    }
}
