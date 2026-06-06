using System;
using System.Threading.Tasks;
using SpeedClaim.Api.Context;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;

namespace SpeedClaim.Api.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly SpeedClaimDbContext _context;

    public IUserRepository Users { get; }
    public IPolicyRepository Policies { get; }
    public IClaimRepository Claims { get; }
    
    public IRepository<InsuranceProduct> Products { get; }
    public IRepository<Agent> Agents { get; }
    public IRepository<DocumentType> DocumentTypes { get; }
    public IDocumentRepository Documents { get; }
    public IPaymentTransactionRepository PaymentTransactions { get; }
    public IRepository<Role> Roles { get; }
    public IRefreshTokenRepository RefreshTokens { get; }
    public IRepository<ClaimWorkflow> ClaimWorkflows { get; }
    
    public IRepository<AuditLog> AuditLogs { get; }
    public IRepository<UserConsent> UserConsents { get; }
    public IRepository<PremiumSchedule> PremiumSchedules { get; }
    public IRepository<PaymentStatusHistory> PaymentStatusHistories { get; }
    public IRepository<ClaimDocumentChecklist> ClaimDocumentChecklists { get; }

    public UnitOfWork(SpeedClaimDbContext context)
    {
        _context = context;
        
        Users = new UserRepository(_context);
        Policies = new PolicyRepository(_context);
        Claims = new ClaimRepository(_context);
        
        Products = new Repository<InsuranceProduct>(_context);
        Agents = new Repository<Agent>(_context);
        DocumentTypes = new Repository<DocumentType>(_context);
        Documents = new DocumentRepository(_context);
        PaymentTransactions = new PaymentTransactionRepository(_context);
        Roles = new Repository<Role>(_context);
        RefreshTokens = new RefreshTokenRepository(_context);
        ClaimWorkflows = new Repository<ClaimWorkflow>(_context);
        
        AuditLogs = new Repository<AuditLog>(_context);
        UserConsents = new Repository<UserConsent>(_context);
        PremiumSchedules = new Repository<PremiumSchedule>(_context);
        PaymentStatusHistories = new Repository<PaymentStatusHistory>(_context);
        ClaimDocumentChecklists = new Repository<ClaimDocumentChecklist>(_context);
    }

    public async Task<int> CompleteAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
