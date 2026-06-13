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
    public IPremiumPaymentRepository PremiumPayments { get; }
    public ISubmittedDocumentRepository SubmittedDocuments { get; }
    
    public IRepository<Session> Sessions { get; }
    public IRepository<UserToken> UserTokens { get; }
    public IRepository<Surveyor> Surveyors { get; }
    public IRepository<Address> Addresses { get; }
    public IRepository<Customer> Customers { get; }
    public IRepository<CustomerMember> CustomerMembers { get; }
    public IRepository<KycRecord> KycRecords { get; }
    public IRepository<Branch> Branches { get; }
    public IRepository<Agent> Agents { get; }
    public IRepository<AgentCommission> AgentCommissions { get; }
    public IRepository<InsuranceProduct> InsuranceProducts { get; }
    public IRepository<PremiumRateTable> PremiumRateTables { get; }
    public IRepository<Proposal> Proposals { get; }
    public IRepository<ProposalMember> ProposalMembers { get; }
    public IRepository<PolicyMember> PolicyMembers { get; }
    public IRepository<Nominee> Nominees { get; }
    public IRepository<PolicyStatusHistory> PolicyStatusHistories { get; }
    public IRepository<Endorsement> Endorsements { get; }
    public IRepository<HealthDetail> HealthDetails { get; }
    public IRepository<LifeDetail> LifeDetails { get; }
    public IRepository<MotorDetail> MotorDetails { get; }
    public IRepository<StripeCustomer> StripeCustomers { get; }
    public IRepository<PremiumSchedule> PremiumSchedules { get; }
    public IRepository<ClaimStatusHistory> ClaimStatusHistories { get; }
    public IRepository<HealthClaimDetail> HealthClaimDetails { get; }
    public IRepository<LifeClaimDetail> LifeClaimDetails { get; }
    public IRepository<MotorClaimDetail> MotorClaimDetails { get; }
    public IRepository<Grievance> Grievances { get; }
    public IRepository<DocumentRequirement> DocumentRequirements { get; }
    public IRepository<Notification> Notifications { get; }
    public IRepository<EmailTemplate> EmailTemplates { get; }
    public IRepository<EmailLog> EmailLogs { get; }
    public IRepository<AuditLog> AuditLogs { get; }
    public IRepository<SystemConfig> SystemConfigs { get; }
    public IRepository<UserConsent> UserConsents { get; }

    public UnitOfWork(SpeedClaimDbContext context)
    {
        _context = context;
        
        Users = new UserRepository(_context);
        Policies = new PolicyRepository(_context);
        Claims = new ClaimRepository(_context);
        PremiumPayments = new PremiumPaymentRepository(_context);
        SubmittedDocuments = new SubmittedDocumentRepository(_context);
        
        Sessions = new Repository<Session>(_context);
        UserTokens = new Repository<UserToken>(_context);
        Surveyors = new Repository<Surveyor>(_context);
        Addresses = new Repository<Address>(_context);
        Customers = new Repository<Customer>(_context);
        CustomerMembers = new Repository<CustomerMember>(_context);
        KycRecords = new Repository<KycRecord>(_context);
        Branches = new Repository<Branch>(_context);
        Agents = new Repository<Agent>(_context);
        AgentCommissions = new Repository<AgentCommission>(_context);
        InsuranceProducts = new Repository<InsuranceProduct>(_context);
        PremiumRateTables = new Repository<PremiumRateTable>(_context);
        Proposals = new Repository<Proposal>(_context);
        ProposalMembers = new Repository<ProposalMember>(_context);
        PolicyMembers = new Repository<PolicyMember>(_context);
        Nominees = new Repository<Nominee>(_context);
        PolicyStatusHistories = new Repository<PolicyStatusHistory>(_context);
        Endorsements = new Repository<Endorsement>(_context);
        HealthDetails = new Repository<HealthDetail>(_context);
        LifeDetails = new Repository<LifeDetail>(_context);
        MotorDetails = new Repository<MotorDetail>(_context);
        StripeCustomers = new Repository<StripeCustomer>(_context);
        PremiumSchedules = new Repository<PremiumSchedule>(_context);
        ClaimStatusHistories = new Repository<ClaimStatusHistory>(_context);
        HealthClaimDetails = new Repository<HealthClaimDetail>(_context);
        LifeClaimDetails = new Repository<LifeClaimDetail>(_context);
        MotorClaimDetails = new Repository<MotorClaimDetail>(_context);
        Grievances = new Repository<Grievance>(_context);
        DocumentRequirements = new Repository<DocumentRequirement>(_context);
        Notifications = new Repository<Notification>(_context);
        EmailTemplates = new Repository<EmailTemplate>(_context);
        EmailLogs = new Repository<EmailLog>(_context);
        AuditLogs = new Repository<AuditLog>(_context);
        SystemConfigs = new Repository<SystemConfig>(_context);
        UserConsents = new Repository<UserConsent>(_context);
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
