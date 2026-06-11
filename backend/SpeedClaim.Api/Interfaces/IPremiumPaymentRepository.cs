using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpeedClaim.Api.Models;

namespace SpeedClaim.Api.Interfaces;

public interface IPremiumPaymentRepository : IRepository<PremiumPayment>
{
    Task<PremiumPayment?> GetByIntentWithScheduleAsync(string intentId);
    Task<IEnumerable<PremiumPayment>> GetByUserIdAsync(Guid userId);
}
