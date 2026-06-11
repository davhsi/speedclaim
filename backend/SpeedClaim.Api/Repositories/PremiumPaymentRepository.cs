using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SpeedClaim.Api.Context;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;

namespace SpeedClaim.Api.Repositories;

public class PremiumPaymentRepository : Repository<PremiumPayment>, IPremiumPaymentRepository
{
    public PremiumPaymentRepository(SpeedClaimDbContext context) : base(context)
    {
    }

    public async Task<PremiumPayment?> GetByIntentWithScheduleAsync(string intentId)
    {
        return await Context.PremiumPayments
            .Include(t => t.Schedule)
            .SingleOrDefaultAsync(t => t.StripePaymentIntentId == intentId);
    }

    public async Task<IEnumerable<PremiumPayment>> GetByUserIdAsync(Guid userId)
    {
        return await Context.PremiumPayments
            .Include(t => t.Schedule)
            .ThenInclude(s => s!.Policy!)
            .Where(t => t.Schedule!.Policy!.Customer!.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }
}
