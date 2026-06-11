using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SpeedClaim.Api.Models;

namespace SpeedClaim.Api.Context;

public partial class SpeedClaimDbContext : DbContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SpeedClaimDbContext(DbContextOptions<SpeedClaimDbContext> options, IHttpContextAccessor httpContextAccessor = null!) : base(options) 
    { 
        _httpContextAccessor = httpContextAccessor;
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Session> Sessions { get; set; } = null!;
    public DbSet<UserToken> UserTokens { get; set; } = null!;
    public DbSet<Surveyor> Surveyors { get; set; } = null!;
    public DbSet<Address> Addresses { get; set; } = null!;
    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<CustomerMember> CustomerMembers { get; set; } = null!;
    public DbSet<KycRecord> KycRecords { get; set; } = null!;
    public DbSet<Branch> Branches { get; set; } = null!;
    public DbSet<Agent> Agents { get; set; } = null!;
    public DbSet<AgentCommission> AgentCommissions { get; set; } = null!;
    public DbSet<InsuranceProduct> InsuranceProducts { get; set; } = null!;
    public DbSet<PremiumRateTable> PremiumRateTables { get; set; } = null!;
    public DbSet<Proposal> Proposals { get; set; } = null!;
    public DbSet<ProposalMember> ProposalMembers { get; set; } = null!;
    public DbSet<Policy> Policies { get; set; } = null!;
    public DbSet<PolicyMember> PolicyMembers { get; set; } = null!;
    public DbSet<Nominee> Nominees { get; set; } = null!;
    public DbSet<PolicyStatusHistory> PolicyStatusHistories { get; set; } = null!;
    public DbSet<Endorsement> Endorsements { get; set; } = null!;
    public DbSet<HealthDetail> HealthDetails { get; set; } = null!;
    public DbSet<LifeDetail> LifeDetails { get; set; } = null!;
    public DbSet<MotorDetail> MotorDetails { get; set; } = null!;
    public DbSet<StripeCustomer> StripeCustomers { get; set; } = null!;
    public DbSet<PremiumSchedule> PremiumSchedules { get; set; } = null!;
    public DbSet<PremiumPayment> PremiumPayments { get; set; } = null!;
    public DbSet<Claim> Claims { get; set; } = null!;
    public DbSet<ClaimStatusHistory> ClaimStatusHistories { get; set; } = null!;
    public DbSet<HealthClaimDetail> HealthClaimDetails { get; set; } = null!;
    public DbSet<LifeClaimDetail> LifeClaimDetails { get; set; } = null!;
    public DbSet<MotorClaimDetail> MotorClaimDetails { get; set; } = null!;
    public DbSet<Grievance> Grievances { get; set; } = null!;
    public DbSet<DocumentRequirement> DocumentRequirements { get; set; } = null!;
    public DbSet<SubmittedDocument> SubmittedDocuments { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;
    public DbSet<EmailTemplate> EmailTemplates { get; set; } = null!;
    public DbSet<EmailLog> EmailLogs { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public DbSet<SystemConfig> SystemConfigs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Convert all property and table names to snake_case to match postgres conventions
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            var tableName = entity.GetTableName();
            if (tableName != null)
                entity.SetTableName(ToSnakeCase(tableName));

            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.Name));
            }
        }

        // Configuration for Auth & Users
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_users");
            e.HasIndex(x => x.Email).IsUnique().HasDatabaseName("uq_users_email");
            e.HasIndex(x => x.Phone).IsUnique().HasDatabaseName("uq_users_phone");
            e.Property(x => x.Email).IsRequired().HasMaxLength(255);
            e.Property(x => x.Phone).HasMaxLength(20);
            e.Property(x => x.PasswordHash).IsRequired();
            e.Property(x => x.Salutation).HasMaxLength(10).HasConversion<string>();
            e.Property(x => x.FirstName).IsRequired().HasMaxLength(100);
            e.Property(x => x.LastName).IsRequired().HasMaxLength(100);
            e.Property(x => x.Role).HasMaxLength(50).HasConversion<string>();
            e.Ignore(x => x.FullName);
            
            e.HasData(
                new User
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Email = "davish.std@gmail.com",
                    PasswordHash = "$2a$11$O.SrE3dQ5wjiWGc20uH7HuIuFwnrCYiuROffc9k/nk6./69E2z2.S",
                    Salutation = SpeedClaim.Api.Models.Enums.Salutation.Mr,
                    FirstName = "Davish",
                    LastName = "Dev",
                    Phone = "9998887776",
                    Role = SpeedClaim.Api.Models.Enums.UserRole.Admin,
                    IsEmailVerified = true,
                    CreatedAt = new DateTimeOffset(2026, 6, 7, 0, 0, 0, TimeSpan.Zero),
                    IsActive = true
                }
            );
        });

        modelBuilder.Entity<Session>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_sessions");
            e.Property(x => x.RefreshTokenHash).IsRequired();
            e.Property(x => x.IpAddress).HasMaxLength(45);
            e.HasOne(x => x.User).WithMany(u => u.Sessions).HasForeignKey(x => x.UserId).HasConstraintName("FK_sessions_users_user_id").OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserToken>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_user_tokens");
            e.Property(x => x.TokenType).HasMaxLength(50).HasConversion<string>();
            e.HasOne(x => x.User).WithMany(u => u.UserTokens).HasForeignKey(x => x.UserId).HasConstraintName("FK_user_tokens_users_user_id").OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Address>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_addresses");
            e.Property(x => x.AddressLine1).IsRequired().HasMaxLength(200);
            e.Property(x => x.AddressLine2).HasMaxLength(200);
            e.Property(x => x.City).IsRequired().HasMaxLength(100);
            e.Property(x => x.State).IsRequired().HasMaxLength(100);
            e.Property(x => x.Pincode).IsRequired().HasMaxLength(20);
            e.Property(x => x.Country).IsRequired().HasMaxLength(100);
            e.Property(x => x.AddressType).HasMaxLength(50).HasConversion<string>();

            e.HasOne(x => x.User).WithMany(u => u.Addresses).HasForeignKey(x => x.UserId).HasConstraintName("FK_addresses_users_user_id").OnDelete(DeleteBehavior.Cascade);
        });

        // Customers & Family
        modelBuilder.Entity<Customer>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_customers");
            e.Property(x => x.Occupation).HasMaxLength(100);
            e.Property(x => x.AnnualIncome).HasColumnType("decimal(15,2)");
            e.Property(x => x.Gender).HasConversion<string>();
            e.Property(x => x.MaritalStatus).HasConversion<string>();
            e.HasOne(x => x.User).WithOne(u => u.Customer).HasForeignKey<Customer>(x => x.UserId).HasConstraintName("FK_customers_users_user_id").OnDelete(DeleteBehavior.Cascade);
            
            e.HasData(
                new Customer
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    DateOfBirth = new DateTime(1995, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    Gender = SpeedClaim.Api.Models.Enums.Gender.Male,
                    MaritalStatus = SpeedClaim.Api.Models.Enums.MaritalStatus.Single,
                    Occupation = "Software Engineer",
                    AnnualIncome = 150000m,
                    CreatedAt = new DateTimeOffset(2026, 6, 7, 0, 0, 0, TimeSpan.Zero)
                }
            );
        });

        modelBuilder.Entity<CustomerMember>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_customer_members");
            e.Property(x => x.Salutation).HasMaxLength(20).HasConversion<string>();
            e.Property(x => x.FirstName).HasMaxLength(50);
            e.Property(x => x.LastName).HasMaxLength(50);
            e.Property(x => x.Gender).HasConversion<string>();
            e.Property(x => x.Relationship).HasConversion<string>();
            e.Ignore(x => x.FullName);
            e.HasOne(x => x.Customer).WithMany(c => c.CustomerMembers).HasForeignKey(x => x.CustomerId).HasConstraintName("FK_customer_members_customers_customer_id").OnDelete(DeleteBehavior.Cascade);
        });

        // KYC
        modelBuilder.Entity<KycRecord>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_kyc_records");
            e.HasIndex(x => new { x.IdType, x.IdNumber }).IsUnique().HasDatabaseName("uq_kyc_records_id_type_number");
            e.Property(x => x.IdNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.IdType).HasConversion<string>();
            e.Property(x => x.KycStatus).HasConversion<string>();
            e.HasOne(x => x.User).WithOne(u => u.KycRecord).HasForeignKey<KycRecord>(x => x.UserId).HasConstraintName("FK_kyc_records_users_user_id").OnDelete(DeleteBehavior.Cascade);
            
            e.HasData(
                new KycRecord
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    IdType = SpeedClaim.Api.Models.Enums.IdType.Aadhaar,
                    IdNumber = "999988887777",
                    KycStatus = SpeedClaim.Api.Models.Enums.KycStatus.Approved,
                    CreatedAt = new DateTimeOffset(2026, 6, 7, 0, 0, 0, TimeSpan.Zero)
                }
            );
        });

        // Branches & Agents
        modelBuilder.Entity<Branch>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_branches");
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.Property(x => x.City).HasMaxLength(100);
            e.Property(x => x.State).HasMaxLength(100);
            e.Property(x => x.Phone).HasMaxLength(20);
            e.Property(x => x.Email).HasMaxLength(255);
        });

        modelBuilder.Entity<Agent>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_agents");
            e.HasIndex(x => x.UserId).IsUnique().HasDatabaseName("uq_agents_user_id");
            e.Property(x => x.AgentCode).HasMaxLength(20);
            e.Property(x => x.LicenseNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.LicenseExpiry).HasColumnType("date");
            e.Property(x => x.CommissionRate).HasColumnType("decimal(5,2)");
            e.Property(x => x.AgentType).HasConversion<string>();
            e.HasOne(x => x.User).WithOne(u => u.Agent).HasForeignKey<Agent>(x => x.UserId).HasConstraintName("FK_agents_users_user_id").OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).HasConstraintName("FK_agents_branches_branch_id");
        });

        modelBuilder.Entity<AgentCommission>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_agent_commissions");
            e.Property(x => x.CommissionRate).HasColumnType("decimal(5,2)");
            e.Property(x => x.CommissionAmount).HasColumnType("decimal(10,2)");
            e.HasOne(x => x.Agent).WithMany().HasForeignKey(x => x.AgentId).HasConstraintName("FK_agent_commissions_agents_agent_id").OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Policy).WithMany().HasForeignKey(x => x.PolicyId).HasConstraintName("FK_agent_commissions_policies_policy_id").OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.PremiumPayment).WithMany().HasForeignKey(x => x.PremiumPaymentId).HasConstraintName("FK_agent_commissions_premium_payments_payment_id").OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Surveyor>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_surveyors");
            e.HasIndex(x => x.UserId).IsUnique().HasDatabaseName("uq_surveyors_user_id");
            e.Property(x => x.LicenseNumber).HasMaxLength(50);
            e.Property(x => x.LicenseExpiry).HasColumnType("date");
            e.Property(x => x.SurveyorType).HasConversion<string>();
            e.Property(x => x.Specialization).HasConversion<string>();
            e.HasOne(x => x.User).WithOne(u => u.Surveyor).HasForeignKey<Surveyor>(x => x.UserId).HasConstraintName("FK_surveyors_users_user_id").OnDelete(DeleteBehavior.Cascade);
        });

        // Products & Proposals
        modelBuilder.Entity<InsuranceProduct>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_insurance_products");
            e.HasIndex(x => x.Uin).IsUnique().HasDatabaseName("uq_insurance_products_uin");
            e.Property(x => x.ProductName).HasMaxLength(150);
            e.Property(x => x.Uin).HasMaxLength(50);
            e.Property(x => x.MinSumAssured).HasColumnType("decimal(15,2)");
            e.Property(x => x.MaxSumAssured).HasColumnType("decimal(15,2)");

            e.HasData(
                new InsuranceProduct { Id = Guid.Parse("10000000-1111-1111-1111-111111111111"), ProductName = "Term Life Basic", Domain = "LIFE", Uin = "UIN123", Description = "Basic term life insurance", MinAge = 18, MaxAge = 60, MinSumAssured = 100000m, MaxSumAssured = 5000000m, MinTenureYears = 5, MaxTenureYears = 30, WaitingPeriodDays = 0, AllowsFamilyFloater = false, MaxFamilyMembers = 1, IsActive = true, CreatedAt = new DateTimeOffset(2026, 6, 7, 0, 0, 0, TimeSpan.Zero) }
            );
        });

        modelBuilder.Entity<PremiumRateTable>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_premium_rate_tables");
            e.Property(x => x.SumAssuredMin).HasColumnType("decimal(15,2)");
            e.Property(x => x.SumAssuredMax).HasColumnType("decimal(15,2)");
            e.Property(x => x.AnnualPremium).HasColumnType("decimal(10,2)");
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).HasConstraintName("FK_premium_rate_tables_products");
        });

        modelBuilder.Entity<Proposal>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_proposals");
            e.HasIndex(x => x.ProposalNumber).IsUnique().HasDatabaseName("uq_proposals_proposal_number");
            e.Property(x => x.ProposalNumber).HasMaxLength(30);
            e.Property(x => x.SumAssured).HasColumnType("decimal(15,2)");
            e.Property(x => x.PremiumAmount).HasColumnType("decimal(10,2)");
            e.Property(x => x.PolicyType).HasConversion<string>();
            e.Property(x => x.Status).HasMaxLength(50).HasConversion<string>();
            e.HasOne(x => x.Customer).WithMany(c => c.Proposals).HasForeignKey(x => x.CustomerId).HasConstraintName("FK_proposals_customers_customer_id").OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Agent).WithMany().HasForeignKey(x => x.AgentId).HasConstraintName("FK_proposals_agents_agent_id").OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).HasConstraintName("FK_proposals_products_product_id").OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Underwriter).WithMany().HasForeignKey(x => x.UnderwriterId).HasConstraintName("FK_proposals_users_underwriter_id").OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ProposalMember>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_proposal_members");
            e.HasOne(x => x.Proposal).WithMany(p => p.ProposalMembers).HasForeignKey(x => x.ProposalId).HasConstraintName("FK_proposal_members_proposals_proposal_id").OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.CustomerMember).WithMany().HasForeignKey(x => x.CustomerMemberId).HasConstraintName("FK_proposal_members_customer_members_customer_member_id").OnDelete(DeleteBehavior.Restrict);
        });

        // Policies Architecture
        modelBuilder.Entity<Policy>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_policies");
            e.HasIndex(x => x.PolicyNumber).IsUnique().HasDatabaseName("uq_policies_policy_number");
            e.Property(x => x.PolicyNumber).HasMaxLength(30);
            e.Property(x => x.PolicyType).HasConversion<string>();
            e.Property(x => x.Status).HasMaxLength(50).HasConversion<string>();
            e.Property(x => x.SumAssured).HasColumnType("decimal(15,2)");
            e.Property(x => x.PremiumAmount).HasColumnType("decimal(10,2)");
            e.HasOne(x => x.Proposal).WithOne().HasForeignKey<Policy>(x => x.ProposalId).HasConstraintName("FK_policies_proposals_proposal_id").OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Customer).WithMany(c => c.Policies).HasForeignKey(x => x.CustomerId).HasConstraintName("FK_policies_customers_customer_id").OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Agent).WithMany().HasForeignKey(x => x.AgentId).HasConstraintName("FK_policies_agents_agent_id").OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).HasConstraintName("FK_policies_products_product_id").OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PolicyMember>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_policy_members");
            e.HasOne(x => x.Policy).WithMany(p => p.PolicyMembers).HasForeignKey(x => x.PolicyId).HasConstraintName("FK_policy_members_policies").OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Nominee>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_nominees");
            e.Property(x => x.FullName).HasMaxLength(100);
            e.Property(x => x.Relationship).HasMaxLength(50);
            e.Property(x => x.SharePercentage).HasColumnType("decimal(5,2)");
            e.Property(x => x.AppointeeName).HasMaxLength(100);
            e.HasOne(x => x.Proposal).WithMany(p => p.Nominees).HasForeignKey(x => x.ProposalId).HasConstraintName("FK_nominees_proposals").OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Policy).WithMany(p => p.Nominees).HasForeignKey(x => x.PolicyId).HasConstraintName("FK_nominees_policies").OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PolicyStatusHistory>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_policy_status_history");
            e.Property(x => x.OldStatus).HasMaxLength(50).HasConversion<string>();
            e.Property(x => x.NewStatus).HasMaxLength(50).HasConversion<string>();
            e.HasOne(x => x.Policy).WithMany(p => p.StatusHistory).HasForeignKey(x => x.PolicyId).HasConstraintName("FK_policy_status_history_policies_policy_id").OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ChangedBy).WithMany().HasForeignKey(x => x.ChangedById).HasConstraintName("FK_policy_status_history_users_changed_by_id").OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Endorsement>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_endorsements");
            e.Property(x => x.EndorsementType).HasConversion<string>();
            e.Property(x => x.OldValue).HasColumnType("jsonb");
            e.Property(x => x.NewValue).HasColumnType("jsonb");
            e.Property(x => x.Status).HasMaxLength(50).HasConversion<string>();
            e.HasOne(x => x.Policy).WithMany(p => p.Endorsements).HasForeignKey(x => x.PolicyId).HasConstraintName("FK_endorsements_policies_policy_id").OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.RequestedBy).WithMany().HasForeignKey(x => x.RequestedById).HasConstraintName("FK_endorsements_users_requested_by").OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ReviewedBy).WithMany().HasForeignKey(x => x.ReviewedById).HasConstraintName("FK_endorsements_users_reviewed_by").OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<HealthDetail>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_health_details");
            e.Property(x => x.TpaName).HasMaxLength(100);
            e.Property(x => x.RoomRentLimit).HasColumnType("decimal(10,2)");
            e.Property(x => x.CopayPercentage).HasColumnType("decimal(5,2)");
            e.HasOne(x => x.Proposal).WithOne(p => p.HealthDetail).HasForeignKey<HealthDetail>(x => x.ProposalId).HasConstraintName("FK_health_details_proposals").OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Policy).WithOne(p => p.HealthDetail).HasForeignKey<HealthDetail>(x => x.PolicyId).HasConstraintName("FK_health_details_policies").OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<LifeDetail>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_life_details");
            e.Property(x => x.MaturityBenefit).HasColumnType("decimal(15,2)");
            e.Property(x => x.DeathBenefit).HasColumnType("decimal(15,2)");
            e.HasOne(x => x.Proposal).WithOne(p => p.LifeDetail).HasForeignKey<LifeDetail>(x => x.ProposalId).HasConstraintName("FK_life_details_proposals").OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Policy).WithOne(p => p.LifeDetail).HasForeignKey<LifeDetail>(x => x.PolicyId).HasConstraintName("FK_life_details_policies").OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<MotorDetail>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_motor_details");
            e.Property(x => x.VehicleNumber).HasMaxLength(20);
            e.Property(x => x.VehicleMake).HasMaxLength(50);
            e.Property(x => x.VehicleModel).HasMaxLength(50);
            e.Property(x => x.Idv).HasColumnType("decimal(10,2)");
            e.Property(x => x.EngineNumber).HasMaxLength(50);
            e.Property(x => x.ChassisNumber).HasMaxLength(50);
            e.HasOne(x => x.Proposal).WithOne(p => p.MotorDetail).HasForeignKey<MotorDetail>(x => x.ProposalId).HasConstraintName("FK_motor_details_proposals").OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Policy).WithOne(p => p.MotorDetail).HasForeignKey<MotorDetail>(x => x.PolicyId).HasConstraintName("FK_motor_details_policies").OnDelete(DeleteBehavior.SetNull);
        });

        // Payments
        modelBuilder.Entity<StripeCustomer>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_stripe_customers");
            e.HasIndex(x => x.StripeCustomerId).IsUnique();
            e.Property(x => x.StripeCustomerId).HasMaxLength(100);
            e.HasOne(x => x.User).WithOne().HasForeignKey<StripeCustomer>(x => x.UserId).HasConstraintName("FK_stripe_customers_users_user_id").OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PremiumSchedule>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_premium_schedules");
            e.Property(x => x.Amount).HasColumnType("decimal(10,2)");
            e.Property(x => x.Status).HasMaxLength(50).HasConversion<string>();
            e.HasOne(x => x.Proposal).WithMany(p => p.PremiumSchedules).HasForeignKey(x => x.ProposalId).HasConstraintName("FK_premium_schedules_proposals").OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Policy).WithMany(p => p.PremiumSchedules).HasForeignKey(x => x.PolicyId).HasConstraintName("FK_premium_schedules_policies").OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PremiumPayment>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_premium_payments");
            e.Property(x => x.Amount).HasColumnType("decimal(10,2)");
            e.Property(x => x.Currency).HasMaxLength(3);
            e.Property(x => x.StripePaymentIntentId).HasMaxLength(255);
            e.Property(x => x.StripeChargeId).HasMaxLength(255);
            e.Property(x => x.PaymentType).HasMaxLength(50).HasConversion<string>();
            e.Property(x => x.Status).HasMaxLength(50).HasConversion<string>();
            e.Property(x => x.PaymentMethod).HasMaxLength(50).HasConversion<string>();
            e.HasOne(x => x.Proposal).WithMany(p => p.PremiumPayments).HasForeignKey(x => x.ProposalId).HasConstraintName("FK_premium_payments_proposals").OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Policy).WithMany(p => p.PremiumPayments).HasForeignKey(x => x.PolicyId).HasConstraintName("FK_premium_payments_policies").OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Schedule).WithOne(s => s.Payment).HasForeignKey<PremiumPayment>(x => x.ScheduleId).HasConstraintName("FK_premium_payments_schedules");
        });

        // Claims
        modelBuilder.Entity<Claim>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_claims");
            e.HasIndex(x => x.ClaimNumber).IsUnique().HasDatabaseName("uq_claims_claim_number");
            e.Property(x => x.ClaimNumber).HasMaxLength(30);
            e.Property(x => x.ClaimAmountRequested).HasColumnType("decimal(15,2)");
            e.Property(x => x.ClaimAmountApproved).HasColumnType("decimal(15,2)");
            e.Property(x => x.ClaimType).HasMaxLength(50).HasConversion<string>();
            e.Property(x => x.Status).HasMaxLength(50).HasConversion<string>();
            e.HasOne(x => x.Policy).WithMany(p => p.Claims).HasForeignKey(x => x.PolicyId).HasConstraintName("FK_claims_policies_policy_id").OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Customer).WithMany(c => c.Claims).HasForeignKey(x => x.CustomerId).HasConstraintName("FK_claims_customers_customer_id").OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ClaimantMember).WithMany().HasForeignKey(x => x.ClaimantMemberId).HasConstraintName("FK_claims_customer_members_claimant_member_id").OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.AssignedOfficer).WithMany().HasForeignKey(x => x.AssignedOfficerId).HasConstraintName("FK_claims_users_assigned_officer_id").OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Surveyor).WithMany(s => s.Claims).HasForeignKey(x => x.SurveyorId).HasConstraintName("FK_claims_surveyors").OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ClaimStatusHistory>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_claim_status_history");
            e.Property(x => x.OldStatus).HasMaxLength(50).HasConversion<string>();
            e.Property(x => x.NewStatus).HasMaxLength(50).HasConversion<string>();
            e.HasOne(x => x.Claim).WithMany(c => c.StatusHistory).HasForeignKey(x => x.ClaimId).HasConstraintName("FK_claim_status_history_claims_claim_id").OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ChangedBy).WithMany().HasForeignKey(x => x.ChangedById).HasConstraintName("FK_claim_status_history_users_changed_by_id").OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<HealthClaimDetail>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_health_claim_details");
            e.Property(x => x.HospitalName).HasMaxLength(150);
            e.Property(x => x.TpaReferenceNumber).HasMaxLength(100);
            e.HasOne(x => x.Claim).WithOne(c => c.HealthDetail).HasForeignKey<HealthClaimDetail>(x => x.ClaimId).HasConstraintName("FK_health_claim_details_claims").OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LifeClaimDetail>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_life_claim_details");
            e.Property(x => x.CauseOfDeath).HasMaxLength(200);
            e.Property(x => x.PlaceOfDeath).HasMaxLength(200);
            e.Property(x => x.ClaimantRelationship).HasMaxLength(50);
            e.Property(x => x.DeathCertificateNumber).HasMaxLength(100);
            e.Property(x => x.CertifyingDoctor).HasMaxLength(100);
            e.Property(x => x.ClaimantName).HasMaxLength(100);
            e.HasOne(x => x.Claim).WithOne(c => c.LifeDetail).HasForeignKey<LifeClaimDetail>(x => x.ClaimId).HasConstraintName("FK_life_claim_details_claims").OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MotorClaimDetail>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_motor_claim_details");
            e.Property(x => x.FirNumber).HasMaxLength(100);
            e.Property(x => x.GarageName).HasMaxLength(150);
            e.Property(x => x.EstimatedRepairCost).HasColumnType("decimal(10,2)");
            e.HasOne(x => x.Claim).WithOne(c => c.MotorDetail).HasForeignKey<MotorClaimDetail>(x => x.ClaimId).HasConstraintName("FK_motor_claim_details_claims").OnDelete(DeleteBehavior.Cascade);
        });

        // Configuration & Support
        modelBuilder.Entity<Grievance>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_grievances");
            e.HasIndex(x => x.GrievanceNumber).IsUnique().HasDatabaseName("uq_grievances_grievance_number");
            e.Property(x => x.GrievanceNumber).HasMaxLength(30);
            e.Property(x => x.Category).HasMaxLength(50).HasConversion<string>();
            e.Property(x => x.Status).HasMaxLength(50).HasConversion<string>();
        });

        modelBuilder.Entity<DocumentRequirement>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_document_requirements");
            e.Property(x => x.EntityType).HasConversion<string>();
            e.Property(x => x.DocumentKey).HasMaxLength(100);
            e.Property(x => x.Label).HasMaxLength(200);
            e.HasOne(x => x.Product)
             .WithMany()
             .HasForeignKey(x => x.ProductId)
             .OnDelete(DeleteBehavior.Cascade)
             .HasConstraintName("FK_document_requirements_insurance_products_ProductId")
             .IsRequired(false);
            e.HasData(
                new DocumentRequirement { Id = Guid.Parse("10000000-0000-0000-0000-000000000001"), DocumentKey = "AADHAAR", Domain = "ALL", Label = "Aadhaar Card", EntityType = SpeedClaim.Api.Models.Enums.EntityType.Kyc, CreatedAt = new DateTimeOffset(2026, 6, 7, 0, 0, 0, TimeSpan.Zero) },
                new DocumentRequirement { Id = Guid.Parse("10000000-0000-0000-0000-000000000002"), DocumentKey = "PAN", Domain = "ALL", Label = "PAN Card", EntityType = SpeedClaim.Api.Models.Enums.EntityType.Kyc, CreatedAt = new DateTimeOffset(2026, 6, 7, 0, 0, 0, TimeSpan.Zero) }
            );
        });

        modelBuilder.Entity<SubmittedDocument>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_submitted_documents");
            e.Property(x => x.EntityType).HasConversion<string>();
            e.Property(x => x.DocumentKey).HasMaxLength(100);
            e.Property(x => x.OriginalFilename).HasMaxLength(255);
            e.Property(x => x.StoredFilename).HasMaxLength(255);
            e.Property(x => x.MimeType).HasMaxLength(100);
            e.HasOne(x => x.Claim).WithMany(x => x.Documents).HasForeignKey(x => x.EntityId).IsRequired(false).HasConstraintName("FK_submitted_documents_claims_claim_id");
        });


        modelBuilder.Entity<Notification>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_notifications");
            e.Property(x => x.Title).HasMaxLength(200);
            e.Property(x => x.Type).HasMaxLength(50).HasConversion<string>();
            e.Property(x => x.RedirectUrl).HasMaxLength(500);
        });

        modelBuilder.Entity<EmailTemplate>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_email_templates");
            e.Property(x => x.TemplateKey).HasMaxLength(100);
            e.Property(x => x.Subject).HasMaxLength(200);
        });

        modelBuilder.Entity<EmailLog>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_email_logs");
            e.Property(x => x.RecipientEmail).HasMaxLength(255);
            e.Property(x => x.TemplateKey).HasMaxLength(100);
            e.Property(x => x.Subject).HasMaxLength(200);
            e.Property(x => x.Status).HasMaxLength(50).HasConversion<string>();
            e.Property(x => x.ProviderMessageId).HasMaxLength(200);
            e.Property(x => x.VariablesUsed).HasColumnType("jsonb");
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_audit_logs");
            e.Property(x => x.Action).HasMaxLength(100);
            e.Property(x => x.EntityType).HasMaxLength(50);
            e.Property(x => x.IpAddress).HasMaxLength(45);
            e.Property(x => x.OldValue).HasColumnType("jsonb");
            e.Property(x => x.NewValue).HasColumnType("jsonb");
        });

        modelBuilder.Entity<SystemConfig>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_system_configs");
            e.Property(x => x.ConfigKey).HasMaxLength(100);
        });
    }
    
    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        var startUnderscores = System.Text.RegularExpressions.Regex.Match(input, @"^_+");
        return startUnderscores + System.Text.RegularExpressions.Regex.Replace(input, @"([a-z0-9])([A-Z])", "$1_$2").ToLower();
    }
}
