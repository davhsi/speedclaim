using System;
using System.Threading.Tasks;
using SpeedClaim.Api.Models;

namespace SpeedClaim.Api.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IPolicyRepository Policies { get; }
    IClaimRepository Claims { get; }
    IPremiumPaymentRepository PremiumPayments { get; }
    ISubmittedDocumentRepository SubmittedDocuments { get; }
    
    IRepository<Session> Sessions { get; }
    IRepository<UserToken> UserTokens { get; }
    IRepository<Surveyor> Surveyors { get; }
    IRepository<Address> Addresses { get; }
    IRepository<Customer> Customers { get; }
    IRepository<CustomerMember> CustomerMembers { get; }
    IRepository<KycRecord> KycRecords { get; }
    IRepository<Branch> Branches { get; }
    IRepository<Agent> Agents { get; }
    IRepository<AgentCommission> AgentCommissions { get; }
    IRepository<InsuranceProduct> InsuranceProducts { get; }
    IRepository<PremiumRateTable> PremiumRateTables { get; }
    IRepository<Proposal> Proposals { get; }
    IRepository<ProposalMember> ProposalMembers { get; }
    IRepository<PolicyMember> PolicyMembers { get; }
    IRepository<Nominee> Nominees { get; }
    IRepository<PolicyStatusHistory> PolicyStatusHistories { get; }
    IRepository<Endorsement> Endorsements { get; }
    IRepository<HealthDetail> HealthDetails { get; }
    IRepository<LifeDetail> LifeDetails { get; }
    IRepository<MotorDetail> MotorDetails { get; }
    IRepository<StripeCustomer> StripeCustomers { get; }
    IRepository<PremiumSchedule> PremiumSchedules { get; }
    IRepository<ClaimStatusHistory> ClaimStatusHistories { get; }
    IRepository<HealthClaimDetail> HealthClaimDetails { get; }
    IRepository<LifeClaimDetail> LifeClaimDetails { get; }
    IRepository<MotorClaimDetail> MotorClaimDetails { get; }
    IRepository<Grievance> Grievances { get; }
    IRepository<DocumentRequirement> DocumentRequirements { get; }
    IRepository<Notification> Notifications { get; }
    IRepository<EmailTemplate> EmailTemplates { get; }
    IRepository<EmailLog> EmailLogs { get; }
    IRepository<AuditLog> AuditLogs { get; }
    IRepository<SystemConfig> SystemConfigs { get; }

    Task<int> CompleteAsync();
}
