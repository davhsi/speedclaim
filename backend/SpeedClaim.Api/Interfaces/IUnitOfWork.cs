using System;
using System.Threading.Tasks;
using SpeedClaim.Api.Models;

namespace SpeedClaim.Api.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IPolicyRepository Policies { get; }
    IClaimRepository Claims { get; }
    
    IRepository<InsuranceProduct> Products { get; }
    IRepository<Agent> Agents { get; }
    IRepository<DocumentType> DocumentTypes { get; }
    IDocumentRepository Documents { get; }
    IPaymentTransactionRepository PaymentTransactions { get; }
    IRepository<Role> Roles { get; }
    IRefreshTokenRepository RefreshTokens { get; }
    IRepository<ClaimWorkflow> ClaimWorkflows { get; }
    
    IRepository<AuditLog> AuditLogs { get; }
    IRepository<UserConsent> UserConsents { get; }
    IRepository<PremiumSchedule> PremiumSchedules { get; }
    IRepository<PaymentStatusHistory> PaymentStatusHistories { get; }
    IRepository<ClaimDocumentChecklist> ClaimDocumentChecklists { get; }

    Task<int> CompleteAsync();
}
