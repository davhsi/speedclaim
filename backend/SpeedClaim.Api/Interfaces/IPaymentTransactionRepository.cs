using System.Threading.Tasks;
using SpeedClaim.Api.Models;

namespace SpeedClaim.Api.Interfaces;

public interface IPaymentTransactionRepository : IRepository<PaymentTransaction>
{
    Task<PaymentTransaction?> GetByIntentWithPolicyAsync(string intentId);
}
