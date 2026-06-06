using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SpeedClaim.Api.Context;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;

namespace SpeedClaim.Api.Repositories;

public class PaymentTransactionRepository : Repository<PaymentTransaction>, IPaymentTransactionRepository
{
    public PaymentTransactionRepository(SpeedClaimDbContext context) : base(context)
    {
    }

    public async Task<PaymentTransaction?> GetByIntentWithPolicyAsync(string intentId)
    {
        return await Context.PaymentTransactions
            .Include(t => t.Policy)
            .SingleOrDefaultAsync(t => t.StripePaymentIntentId == intentId);
    }
}
