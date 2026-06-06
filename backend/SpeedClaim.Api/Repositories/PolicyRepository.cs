using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SpeedClaim.Api.Context;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;

namespace SpeedClaim.Api.Repositories;

public class PolicyRepository : Repository<Policy>, IPolicyRepository
{
    public PolicyRepository(SpeedClaimDbContext context) : base(context)
    {
    }

    public async Task<VehiclePolicy?> GetVehiclePolicyByIdAsync(Guid id)
    {
        return await Context.Policies.OfType<VehiclePolicy>()
            .Include(p => p.Product)
            .SingleOrDefaultAsync(p => p.Id == id);
    }

    public async Task<HealthPolicy?> GetHealthPolicyByIdAsync(Guid id)
    {
        return await Context.Policies.OfType<HealthPolicy>()
            .Include(p => p.Product)
            .Include(p => p.InsuredMembers)
            .SingleOrDefaultAsync(p => p.Id == id);
    }

    public async Task<LifePolicy?> GetLifePolicyByIdAsync(Guid id)
    {
        return await Context.Policies.OfType<LifePolicy>()
            .Include(p => p.Product)
            .SingleOrDefaultAsync(p => p.Id == id);
    }

    public override async Task<Policy?> GetByIdAsync(Guid id)
    {
        return await Context.Policies
            .Include(p => p.Product)
            .SingleOrDefaultAsync(p => p.Id == id);
    }
}
