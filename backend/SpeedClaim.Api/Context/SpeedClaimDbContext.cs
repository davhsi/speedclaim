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
    public DbSet<ProductBrochure> ProductBrochures { get; set; } = null!;
    public DbSet<PremiumRateTable> PremiumRateTables { get; set; } = null!;
    public DbSet<Proposal> Proposals { get; set; } = null!;
    public DbSet<ProposalMember> ProposalMembers { get; set; } = null!;
    public DbSet<Policy> Policies { get; set; } = null!;
    public DbSet<PolicyAssistantConversation> PolicyAssistantConversations { get; set; } = null!;
    public DbSet<PolicyAssistantMessage> PolicyAssistantMessages { get; set; } = null!;
    public DbSet<SpeedyWorkspaceConversation> SpeedyWorkspaceConversations { get; set; } = null!;
    public DbSet<SpeedyWorkspaceMessage> SpeedyWorkspaceMessages { get; set; } = null!;
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
    public DbSet<UserConsent> UserConsents { get; set; } = null!;
    public DbSet<ProcessedWebhookEvent> ProcessedWebhookEvents { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Global soft-delete filters — automatically exclude logically deleted rows from every query
        modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
        modelBuilder.Entity<Policy>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<Claim>().HasQueryFilter(c => !c.IsDeleted);
        modelBuilder.Entity<Proposal>().HasQueryFilter(pr => !pr.IsDeleted);

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
                    PasswordHash = "$2a$11$noTUuePjq5y/ldIskdG1JOVh7IxShG0RPMr3OEK8Mc6eXPKa3WTWK",
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
            e.HasOne(x => x.OnboardingAgent).WithMany().HasForeignKey(x => x.OnboardingAgentId).HasConstraintName("FK_customers_agents_onboarding_agent_id").OnDelete(DeleteBehavior.SetNull);

            e.HasData(
                new Customer
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    DateOfBirth = new DateOnly(1995, 1, 1),
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
            e.Property(x => x.KycStatus).HasConversion<string>();
            e.HasOne(x => x.User).WithOne(u => u.KycRecord).HasForeignKey<KycRecord>(x => x.UserId).HasConstraintName("FK_kyc_records_users_user_id").OnDelete(DeleteBehavior.Cascade);
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
            e.Property(x => x.CoverageOptionsJson).HasColumnType("jsonb").HasDefaultValue("[]");
            e.Property(x => x.SumAssuredIncrement).HasColumnType("decimal(15,2)");

            e.HasData(
                new InsuranceProduct { Id = Guid.Parse("71000000-0000-0000-0000-000000000001"), ProductName = "CareNest Family Shield", Domain = "Health", Uin = "UIN-HC-DEMO-2026-01", Description = "Fictional family health cover for catalog, quotation, and policy-Q&A testing.", MinAge = 18, MaxAge = 60, MinSumAssured = 300000m, MaxSumAssured = 1500000m, CoverageOptionsJson = "[300000,500000,1000000,1500000]", MinTenureYears = 1, MaxTenureYears = 1, WaitingPeriodDays = 30, AllowsFamilyFloater = true, MaxFamilyMembers = 6, IsActive = true, IsAvailableForSale = true, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) },
                new InsuranceProduct { Id = Guid.Parse("71000000-0000-0000-0000-000000000002"), ProductName = "Horizon Term Protect", Domain = "Life", Uin = "UIN-LI-DEMO-2026-01", Description = "Fictional pure-protection term cover for catalog, quotation, and policy-Q&A testing.", MinAge = 18, MaxAge = 55, MinSumAssured = 2500000m, MaxSumAssured = 10000000m, CoverageOptionsJson = "[]", SumAssuredIncrement = 2500000m, MinTenureYears = 10, MaxTenureYears = 30, WaitingPeriodDays = 0, AllowsFamilyFloater = false, MaxFamilyMembers = 1, IsActive = true, IsAvailableForSale = true, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) },
                new InsuranceProduct { Id = Guid.Parse("71000000-0000-0000-0000-000000000003"), ProductName = "DriveSure Comprehensive", Domain = "Motor", Uin = "UIN-MO-DEMO-2026-01", Description = "Fictional private-car comprehensive cover for catalog, quotation, and claims-flow testing.", MinAge = 18, MaxAge = 75, MinSumAssured = 100000m, MaxSumAssured = 2000000m, CoverageOptionsJson = "[]", MinTenureYears = 1, MaxTenureYears = 1, WaitingPeriodDays = 0, MotorVehicleType = "PrivateCar", AllowsFamilyFloater = false, MaxFamilyMembers = 1, IsActive = true, IsAvailableForSale = true, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) }
            );
        });

        modelBuilder.Entity<ProductBrochure>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_product_brochures");
            e.HasIndex(x => new { x.ProductId, x.Version })
                .IsUnique()
                .HasDatabaseName("uq_product_brochures_product_version");
            e.HasIndex(x => new { x.ProductId, x.ContentHash })
                .IsUnique()
                .HasDatabaseName("uq_product_brochures_product_content_hash");
            e.HasIndex(x => x.ProductId)
                .IsUnique()
                .HasFilter("\"status\" = 'Published'")
                .HasDatabaseName("uq_product_brochures_current_published");
            e.Property(x => x.Version).IsRequired().HasMaxLength(32);
            e.Property(x => x.OriginalFilename).IsRequired().HasMaxLength(255);
            e.Property(x => x.BlobPath).IsRequired().HasMaxLength(1024);
            e.Property(x => x.MimeType).IsRequired().HasMaxLength(100);
            e.Property(x => x.ContentHash).IsRequired().HasMaxLength(64);
            e.Property(x => x.Status).IsRequired().HasMaxLength(32).HasConversion<string>();
            e.Property(x => x.EmbeddingProvider).HasMaxLength(100);
            e.Property(x => x.EmbeddingModel).HasMaxLength(255);
            e.Property(x => x.IngestionErrorCode).HasMaxLength(100);
            e.HasOne(x => x.Product)
                .WithMany(x => x.Brochures)
                .HasForeignKey(x => x.ProductId)
                .HasConstraintName("FK_product_brochures_products_product_id")
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CreatedBy)
                .WithMany()
                .HasForeignKey(x => x.CreatedById)
                .HasConstraintName("FK_product_brochures_users_created_by_id")
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.PublishedBy)
                .WithMany()
                .HasForeignKey(x => x.PublishedById)
                .HasConstraintName("FK_product_brochures_users_published_by_id")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PolicyAssistantConversation>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_policy_assistant_conversations");
            e.HasIndex(x => new { x.PolicyId, x.CreatedByUserId, x.UpdatedAt })
                .HasDatabaseName("ix_policy_assistant_conversations_policy_creator_updated");
            e.HasIndex(x => x.RetainUntil).HasDatabaseName("ix_policy_assistant_conversations_retain_until");
            e.HasOne(x => x.Policy).WithMany(x => x.AssistantConversations).HasForeignKey(x => x.PolicyId)
                .HasConstraintName("FK_policy_assistant_conversations_policies_policy_id").OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Brochure).WithMany().HasForeignKey(x => x.BrochureId)
                .HasConstraintName("FK_policy_assistant_conversations_product_brochures_brochure_id").OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedByUserId)
                .HasConstraintName("FK_policy_assistant_conversations_users_created_by_id").OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PolicyAssistantMessage>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_policy_assistant_messages");
            e.HasIndex(x => new { x.ConversationId, x.CreatedAt }).HasDatabaseName("ix_policy_assistant_messages_conversation_created");
            e.Property(x => x.Role).HasMaxLength(16).HasConversion<string>();
            e.Property(x => x.Content).IsRequired().HasMaxLength(4000);
            e.Property(x => x.EvidenceStatus).HasMaxLength(64);
            e.Property(x => x.Model).HasMaxLength(255);
            e.Property(x => x.PromptVersion).HasMaxLength(100);
            e.HasOne(x => x.Conversation).WithMany(x => x.Messages).HasForeignKey(x => x.ConversationId)
                .HasConstraintName("FK_policy_assistant_messages_conversations_conversation_id").OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SpeedyWorkspaceConversation>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_speedy_workspace_conversations");
            e.Property(x => x.Title).IsRequired().HasMaxLength(120);
            e.HasIndex(x => new { x.CreatedByUserId, x.UpdatedAt }).HasDatabaseName("ix_speedy_workspace_conversations_creator_updated");
            e.HasIndex(x => x.RetainUntil).HasDatabaseName("ix_speedy_workspace_conversations_retain_until");
            e.HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedByUserId)
                .HasConstraintName("FK_speedy_workspace_conversations_users_created_by_id").OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SpeedyWorkspaceMessage>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_speedy_workspace_messages");
            e.Property(x => x.Role).HasMaxLength(16).HasConversion<string>();
            e.Property(x => x.Content).IsRequired().HasMaxLength(8000);
            e.Property(x => x.Intent).HasMaxLength(64);
            e.Property(x => x.Risk).HasMaxLength(32);
            e.Property(x => x.Model).HasMaxLength(255);
            e.HasIndex(x => new { x.ConversationId, x.CreatedAt }).HasDatabaseName("ix_speedy_workspace_messages_conversation_created");
            e.HasOne(x => x.Conversation).WithMany(x => x.Messages).HasForeignKey(x => x.ConversationId)
                .HasConstraintName("FK_speedy_workspace_messages_conversations_conversation_id").OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PremiumRateTable>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_premium_rate_tables");
            e.Property(x => x.SumAssuredMin).HasColumnType("decimal(15,2)");
            e.Property(x => x.SumAssuredMax).HasColumnType("decimal(15,2)");
            e.Property(x => x.AnnualPremium).HasColumnType("decimal(10,2)");
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).HasConstraintName("FK_premium_rate_tables_products");

            e.HasData(
                new PremiumRateTable { Id = Guid.Parse("71100000-0000-0000-0000-000000000001"), ProductId = Guid.Parse("71000000-0000-0000-0000-000000000001"), AgeMin = 18, AgeMax = 30, SumAssuredMin = 300000m, SumAssuredMax = 300000m, AnnualPremium = 4800m, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) },
                new PremiumRateTable { Id = Guid.Parse("71100000-0000-0000-0000-000000000002"), ProductId = Guid.Parse("71000000-0000-0000-0000-000000000001"), AgeMin = 18, AgeMax = 30, SumAssuredMin = 500000m, SumAssuredMax = 500000m, AnnualPremium = 6800m, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) },
                new PremiumRateTable { Id = Guid.Parse("71100000-0000-0000-0000-000000000003"), ProductId = Guid.Parse("71000000-0000-0000-0000-000000000001"), AgeMin = 18, AgeMax = 30, SumAssuredMin = 1000000m, SumAssuredMax = 1000000m, AnnualPremium = 9900m, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) },
                new PremiumRateTable { Id = Guid.Parse("71100000-0000-0000-0000-000000000004"), ProductId = Guid.Parse("71000000-0000-0000-0000-000000000001"), AgeMin = 18, AgeMax = 30, SumAssuredMin = 1500000m, SumAssuredMax = 1500000m, AnnualPremium = 13400m, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) },
                new PremiumRateTable { Id = Guid.Parse("71100000-0000-0000-0000-000000000005"), ProductId = Guid.Parse("71000000-0000-0000-0000-000000000001"), AgeMin = 31, AgeMax = 40, SumAssuredMin = 300000m, SumAssuredMax = 300000m, AnnualPremium = 5900m, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) },
                new PremiumRateTable { Id = Guid.Parse("71100000-0000-0000-0000-000000000006"), ProductId = Guid.Parse("71000000-0000-0000-0000-000000000001"), AgeMin = 31, AgeMax = 40, SumAssuredMin = 500000m, SumAssuredMax = 500000m, AnnualPremium = 8300m, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) },
                new PremiumRateTable { Id = Guid.Parse("71100000-0000-0000-0000-000000000007"), ProductId = Guid.Parse("71000000-0000-0000-0000-000000000001"), AgeMin = 31, AgeMax = 40, SumAssuredMin = 1000000m, SumAssuredMax = 1000000m, AnnualPremium = 12100m, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) },
                new PremiumRateTable { Id = Guid.Parse("71100000-0000-0000-0000-000000000008"), ProductId = Guid.Parse("71000000-0000-0000-0000-000000000001"), AgeMin = 31, AgeMax = 40, SumAssuredMin = 1500000m, SumAssuredMax = 1500000m, AnnualPremium = 16700m, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) },
                new PremiumRateTable { Id = Guid.Parse("71100000-0000-0000-0000-000000000009"), ProductId = Guid.Parse("71000000-0000-0000-0000-000000000001"), AgeMin = 41, AgeMax = 50, SumAssuredMin = 300000m, SumAssuredMax = 300000m, AnnualPremium = 8300m, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) },
                new PremiumRateTable { Id = Guid.Parse("71100000-0000-0000-0000-000000000010"), ProductId = Guid.Parse("71000000-0000-0000-0000-000000000001"), AgeMin = 41, AgeMax = 50, SumAssuredMin = 500000m, SumAssuredMax = 500000m, AnnualPremium = 11900m, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) },
                new PremiumRateTable { Id = Guid.Parse("71100000-0000-0000-0000-000000000011"), ProductId = Guid.Parse("71000000-0000-0000-0000-000000000001"), AgeMin = 41, AgeMax = 50, SumAssuredMin = 1000000m, SumAssuredMax = 1000000m, AnnualPremium = 17600m, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) },
                new PremiumRateTable { Id = Guid.Parse("71100000-0000-0000-0000-000000000012"), ProductId = Guid.Parse("71000000-0000-0000-0000-000000000001"), AgeMin = 41, AgeMax = 50, SumAssuredMin = 1500000m, SumAssuredMax = 1500000m, AnnualPremium = 24300m, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) },
                new PremiumRateTable { Id = Guid.Parse("71100000-0000-0000-0000-000000000013"), ProductId = Guid.Parse("71000000-0000-0000-0000-000000000001"), AgeMin = 51, AgeMax = 60, SumAssuredMin = 300000m, SumAssuredMax = 300000m, AnnualPremium = 12500m, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) },
                new PremiumRateTable { Id = Guid.Parse("71100000-0000-0000-0000-000000000014"), ProductId = Guid.Parse("71000000-0000-0000-0000-000000000001"), AgeMin = 51, AgeMax = 60, SumAssuredMin = 500000m, SumAssuredMax = 500000m, AnnualPremium = 17800m, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) },
                new PremiumRateTable { Id = Guid.Parse("71100000-0000-0000-0000-000000000015"), ProductId = Guid.Parse("71000000-0000-0000-0000-000000000001"), AgeMin = 51, AgeMax = 60, SumAssuredMin = 1000000m, SumAssuredMax = 1000000m, AnnualPremium = 26400m, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) },
                new PremiumRateTable { Id = Guid.Parse("71100000-0000-0000-0000-000000000016"), ProductId = Guid.Parse("71000000-0000-0000-0000-000000000001"), AgeMin = 51, AgeMax = 60, SumAssuredMin = 1500000m, SumAssuredMax = 1500000m, AnnualPremium = 36900m, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) },
                new PremiumRateTable { Id = Guid.Parse("71100000-0000-0000-0000-000000000017"), ProductId = Guid.Parse("71000000-0000-0000-0000-000000000002"), AgeMin = 18, AgeMax = 30, SumAssuredMin = 2500000m, SumAssuredMax = 2500000m, AnnualPremium = 3200m, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) },
                new PremiumRateTable { Id = Guid.Parse("71100000-0000-0000-0000-000000000018"), ProductId = Guid.Parse("71000000-0000-0000-0000-000000000002"), AgeMin = 18, AgeMax = 30, SumAssuredMin = 5000000m, SumAssuredMax = 5000000m, AnnualPremium = 5800m, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) },
                new PremiumRateTable { Id = Guid.Parse("71100000-0000-0000-0000-000000000019"), ProductId = Guid.Parse("71000000-0000-0000-0000-000000000002"), AgeMin = 18, AgeMax = 30, SumAssuredMin = 7500000m, SumAssuredMax = 7500000m, AnnualPremium = 8400m, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) },
                new PremiumRateTable { Id = Guid.Parse("71100000-0000-0000-0000-000000000020"), ProductId = Guid.Parse("71000000-0000-0000-0000-000000000002"), AgeMin = 18, AgeMax = 30, SumAssuredMin = 10000000m, SumAssuredMax = 10000000m, AnnualPremium = 10900m, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) },
                new PremiumRateTable { Id = Guid.Parse("71100000-0000-0000-0000-000000000021"), ProductId = Guid.Parse("71000000-0000-0000-0000-000000000002"), AgeMin = 31, AgeMax = 40, SumAssuredMin = 2500000m, SumAssuredMax = 2500000m, AnnualPremium = 4700m, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) },
                new PremiumRateTable { Id = Guid.Parse("71100000-0000-0000-0000-000000000022"), ProductId = Guid.Parse("71000000-0000-0000-0000-000000000002"), AgeMin = 31, AgeMax = 40, SumAssuredMin = 5000000m, SumAssuredMax = 5000000m, AnnualPremium = 8700m, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) },
                new PremiumRateTable { Id = Guid.Parse("71100000-0000-0000-0000-000000000023"), ProductId = Guid.Parse("71000000-0000-0000-0000-000000000002"), AgeMin = 31, AgeMax = 40, SumAssuredMin = 7500000m, SumAssuredMax = 7500000m, AnnualPremium = 12500m, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) },
                new PremiumRateTable { Id = Guid.Parse("71100000-0000-0000-0000-000000000024"), ProductId = Guid.Parse("71000000-0000-0000-0000-000000000002"), AgeMin = 31, AgeMax = 40, SumAssuredMin = 10000000m, SumAssuredMax = 10000000m, AnnualPremium = 16200m, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) },
                new PremiumRateTable { Id = Guid.Parse("71100000-0000-0000-0000-000000000025"), ProductId = Guid.Parse("71000000-0000-0000-0000-000000000002"), AgeMin = 41, AgeMax = 50, SumAssuredMin = 2500000m, SumAssuredMax = 2500000m, AnnualPremium = 8900m, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) },
                new PremiumRateTable { Id = Guid.Parse("71100000-0000-0000-0000-000000000026"), ProductId = Guid.Parse("71000000-0000-0000-0000-000000000002"), AgeMin = 41, AgeMax = 50, SumAssuredMin = 5000000m, SumAssuredMax = 5000000m, AnnualPremium = 16600m, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) },
                new PremiumRateTable { Id = Guid.Parse("71100000-0000-0000-0000-000000000027"), ProductId = Guid.Parse("71000000-0000-0000-0000-000000000002"), AgeMin = 41, AgeMax = 50, SumAssuredMin = 7500000m, SumAssuredMax = 7500000m, AnnualPremium = 23900m, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) },
                new PremiumRateTable { Id = Guid.Parse("71100000-0000-0000-0000-000000000028"), ProductId = Guid.Parse("71000000-0000-0000-0000-000000000002"), AgeMin = 41, AgeMax = 50, SumAssuredMin = 10000000m, SumAssuredMax = 10000000m, AnnualPremium = 31200m, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) },
                new PremiumRateTable { Id = Guid.Parse("71100000-0000-0000-0000-000000000029"), ProductId = Guid.Parse("71000000-0000-0000-0000-000000000002"), AgeMin = 51, AgeMax = 55, SumAssuredMin = 2500000m, SumAssuredMax = 2500000m, AnnualPremium = 14800m, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) },
                new PremiumRateTable { Id = Guid.Parse("71100000-0000-0000-0000-000000000030"), ProductId = Guid.Parse("71000000-0000-0000-0000-000000000002"), AgeMin = 51, AgeMax = 55, SumAssuredMin = 5000000m, SumAssuredMax = 5000000m, AnnualPremium = 27700m, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) },
                new PremiumRateTable { Id = Guid.Parse("71100000-0000-0000-0000-000000000031"), ProductId = Guid.Parse("71000000-0000-0000-0000-000000000002"), AgeMin = 51, AgeMax = 55, SumAssuredMin = 7500000m, SumAssuredMax = 7500000m, AnnualPremium = 39800m, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) },
                new PremiumRateTable { Id = Guid.Parse("71100000-0000-0000-0000-000000000032"), ProductId = Guid.Parse("71000000-0000-0000-0000-000000000002"), AgeMin = 51, AgeMax = 55, SumAssuredMin = 10000000m, SumAssuredMax = 10000000m, AnnualPremium = 52100m, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) },
                new PremiumRateTable { Id = Guid.Parse("71100000-0000-0000-0000-000000000033"), ProductId = Guid.Parse("71000000-0000-0000-0000-000000000003"), AgeMin = 18, AgeMax = 75, SumAssuredMin = 100000m, SumAssuredMax = 500000m, AnnualPremium = 7200m, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) },
                new PremiumRateTable { Id = Guid.Parse("71100000-0000-0000-0000-000000000034"), ProductId = Guid.Parse("71000000-0000-0000-0000-000000000003"), AgeMin = 18, AgeMax = 75, SumAssuredMin = 500001m, SumAssuredMax = 1000000m, AnnualPremium = 10900m, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) },
                new PremiumRateTable { Id = Guid.Parse("71100000-0000-0000-0000-000000000035"), ProductId = Guid.Parse("71000000-0000-0000-0000-000000000003"), AgeMin = 18, AgeMax = 75, SumAssuredMin = 1000001m, SumAssuredMax = 1500000m, AnnualPremium = 15700m, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) },
                new PremiumRateTable { Id = Guid.Parse("71100000-0000-0000-0000-000000000036"), ProductId = Guid.Parse("71000000-0000-0000-0000-000000000003"), AgeMin = 18, AgeMax = 75, SumAssuredMin = 1500001m, SumAssuredMax = 2000000m, AnnualPremium = 22600m, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) }
            );
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
            e.HasOne(x => x.ProductBrochure).WithMany().HasForeignKey(x => x.ProductBrochureId).HasConstraintName("FK_policies_product_brochures_product_brochure_id").OnDelete(DeleteBehavior.Restrict);
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
            // OldValue/NewValue hold plain scalar values (phone number, address line, sum
            // assured, etc.), not JSON documents. Mapping them as jsonb made Postgres reject
            // any bare string with "invalid input syntax for type json", 500-ing every
            // endorsement request that supplied old/new values. text is the correct type.
            e.Property(x => x.OldValue).HasColumnType("text");
            e.Property(x => x.NewValue).HasColumnType("text");
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
                new DocumentRequirement { Id = Guid.Parse("10000000-0000-0000-0000-000000000002"), DocumentKey = "PAN", Domain = "ALL", Label = "PAN Card", EntityType = SpeedClaim.Api.Models.Enums.EntityType.Kyc, CreatedAt = new DateTimeOffset(2026, 6, 7, 0, 0, 0, TimeSpan.Zero) },
                new DocumentRequirement { Id = Guid.Parse("71200000-0000-0000-0000-000000000001"), ProductId = Guid.Parse("71000000-0000-0000-0000-000000000001"), DocumentKey = "GOVERNMENT_ID", Domain = "Health", Label = "Government photo ID", Description = "Government-issued photo identity document.", EntityType = SpeedClaim.Api.Models.Enums.EntityType.Proposal, IsMandatory = true, IsActive = true, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) },
                new DocumentRequirement { Id = Guid.Parse("71200000-0000-0000-0000-000000000002"), ProductId = Guid.Parse("71000000-0000-0000-0000-000000000001"), DocumentKey = "AGE_PROOF", Domain = "Health", Label = "Age proof", Description = "Document confirming the proposer’s age.", EntityType = SpeedClaim.Api.Models.Enums.EntityType.Proposal, IsMandatory = true, IsActive = true, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) },
                new DocumentRequirement { Id = Guid.Parse("71200000-0000-0000-0000-000000000003"), ProductId = Guid.Parse("71000000-0000-0000-0000-000000000001"), DocumentKey = "MEDICAL_DECLARATION", Domain = "Health", Label = "Medical declaration", Description = "Completed health and medical declaration.", EntityType = SpeedClaim.Api.Models.Enums.EntityType.Proposal, IsMandatory = true, IsActive = true, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) },
                new DocumentRequirement { Id = Guid.Parse("71200000-0000-0000-0000-000000000004"), ProductId = Guid.Parse("71000000-0000-0000-0000-000000000001"), DocumentKey = "PREVIOUS_POLICY", Domain = "Health", Label = "Previous policy copy", Description = "Required only when porting an existing health policy.", EntityType = SpeedClaim.Api.Models.Enums.EntityType.Proposal, IsMandatory = false, IsActive = true, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) },
                new DocumentRequirement { Id = Guid.Parse("71200000-0000-0000-0000-000000000005"), ProductId = Guid.Parse("71000000-0000-0000-0000-000000000002"), DocumentKey = "GOVERNMENT_ID", Domain = "Life", Label = "Government photo ID", Description = "Government-issued photo identity document.", EntityType = SpeedClaim.Api.Models.Enums.EntityType.Proposal, IsMandatory = true, IsActive = true, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) },
                new DocumentRequirement { Id = Guid.Parse("71200000-0000-0000-0000-000000000006"), ProductId = Guid.Parse("71000000-0000-0000-0000-000000000002"), DocumentKey = "PAN", Domain = "Life", Label = "PAN or tax ID", Description = "PAN or other permitted tax identity document.", EntityType = SpeedClaim.Api.Models.Enums.EntityType.Proposal, IsMandatory = true, IsActive = true, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) },
                new DocumentRequirement { Id = Guid.Parse("71200000-0000-0000-0000-000000000007"), ProductId = Guid.Parse("71000000-0000-0000-0000-000000000002"), DocumentKey = "INCOME_PROOF", Domain = "Life", Label = "Income proof", Description = "Required for requested cover above INR 50 lakh.", EntityType = SpeedClaim.Api.Models.Enums.EntityType.Proposal, IsMandatory = false, IsActive = true, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) },
                new DocumentRequirement { Id = Guid.Parse("71200000-0000-0000-0000-000000000008"), ProductId = Guid.Parse("71000000-0000-0000-0000-000000000002"), DocumentKey = "MEDICAL_TESTS", Domain = "Life", Label = "Medical tests", Description = "Required only when requested during underwriting.", EntityType = SpeedClaim.Api.Models.Enums.EntityType.Proposal, IsMandatory = false, IsActive = true, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) },
                new DocumentRequirement { Id = Guid.Parse("71200000-0000-0000-0000-000000000009"), ProductId = Guid.Parse("71000000-0000-0000-0000-000000000003"), DocumentKey = "REGISTRATION_CERTIFICATE", Domain = "Motor", Label = "Registration certificate", Description = "Vehicle registration certificate (RC).", EntityType = SpeedClaim.Api.Models.Enums.EntityType.Proposal, IsMandatory = true, IsActive = true, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) },
                new DocumentRequirement { Id = Guid.Parse("71200000-0000-0000-0000-000000000010"), ProductId = Guid.Parse("71000000-0000-0000-0000-000000000003"), DocumentKey = "DRIVING_LICENCE", Domain = "Motor", Label = "Valid driving licence", Description = "Valid driving licence of the primary driver.", EntityType = SpeedClaim.Api.Models.Enums.EntityType.Proposal, IsMandatory = true, IsActive = true, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) },
                new DocumentRequirement { Id = Guid.Parse("71200000-0000-0000-0000-000000000011"), ProductId = Guid.Parse("71000000-0000-0000-0000-000000000003"), DocumentKey = "PREVIOUS_POLICY", Domain = "Motor", Label = "Existing policy copy", Description = "Required where there is prior motor insurance coverage.", EntityType = SpeedClaim.Api.Models.Enums.EntityType.Proposal, IsMandatory = false, IsActive = true, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) },
                new DocumentRequirement { Id = Guid.Parse("71200000-0000-0000-0000-000000000012"), ProductId = Guid.Parse("71000000-0000-0000-0000-000000000003"), DocumentKey = "VEHICLE_PHOTOS", Domain = "Motor", Label = "Vehicle photographs", Description = "Required when inspection is requested.", EntityType = SpeedClaim.Api.Models.Enums.EntityType.Proposal, IsMandatory = false, IsActive = true, CreatedAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero) }
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
            // EntityId is a polymorphic FK (Proposal or Claim) — no DB-level FK constraint
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
            e.HasData(
                new EmailTemplate
                {
                    Id = Guid.Parse("DD000001-0000-0000-0000-000000000001"),
                    TemplateKey = "EmailVerification",
                    Subject = "Verify Your SpeedClaim Account",
                    BodyHtml = @"<!DOCTYPE html><html lang=""en""><head><meta charset=""UTF-8""><meta name=""viewport"" content=""width=device-width, initial-scale=1.0""><title>Verify your email</title></head><body style=""margin:0;padding:0;background-color:#F7F9FA;font-family:Arial,'Helvetica Neue',Helvetica,sans-serif;""><table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#F7F9FA;padding:32px 16px;""><tr><td align=""center""><table role=""presentation"" width=""480"" cellpadding=""0"" cellspacing=""0"" style=""max-width:480px;width:100%;background-color:#ffffff;border-radius:12px;overflow:hidden;""><tr><td style=""background:linear-gradient(160deg,#0F6E8C 0%,#0A3040 100%);padding:28px 32px;text-align:center;""><img src=""{{logoUrl}}"" width=""28"" height=""28"" alt=""SpeedClaim"" style=""vertical-align:middle;margin-right:8px;border-radius:6px;"" /><span style=""font-size:20px;font-weight:700;color:#ffffff;letter-spacing:-0.3px;vertical-align:middle;"">SpeedClaim</span></td></tr><tr><td style=""padding:32px;""><h1 style=""margin:0 0 16px;font-size:22px;font-weight:600;color:#1A2230;"">Verify your email</h1><p style=""margin:0 0 28px;font-size:15px;line-height:1.6;color:#3C4654;"">Welcome to SpeedClaim! Please verify your email address to activate your account and start managing your insurance policies.</p><table role=""presentation"" cellpadding=""0"" cellspacing=""0"" width=""100%""><tr><td align=""center""><table role=""presentation"" cellpadding=""0"" cellspacing=""0""><tr><td style=""background-color:#0F6E8C;border-radius:8px;""><a href=""{{verifyUrl}}"" target=""_blank"" style=""display:inline-block;padding:14px 36px;font-size:14px;font-weight:600;color:#ffffff;text-decoration:none;"">Verify Email</a></td></tr></table></td></tr></table><p style=""margin:28px 0 0;font-size:13px;line-height:1.5;color:#6B7685;"">This link expires in 24 hours. If you didn't create an account, you can safely ignore this email.</p></td></tr><tr><td style=""padding:20px 32px;border-top:1px solid #E2E6EB;text-align:center;""><p style=""margin:0;font-size:12px;color:#6B7685;"">&copy; {{year}} SpeedClaim. All rights reserved.</p></td></tr></table></td></tr></table></body></html>",
                    IsActive = true,
                    CreatedAt = new DateTimeOffset(2026, 6, 7, 0, 0, 0, TimeSpan.Zero)
                },
                new EmailTemplate
                {
                    Id = Guid.Parse("DD000002-0000-0000-0000-000000000002"),
                    TemplateKey = "PasswordReset",
                    Subject = "Reset Your SpeedClaim Password",
                    BodyHtml = @"<!DOCTYPE html><html lang=""en""><head><meta charset=""UTF-8""><meta name=""viewport"" content=""width=device-width, initial-scale=1.0""><title>Reset your password</title></head><body style=""margin:0;padding:0;background-color:#F7F9FA;font-family:Arial,'Helvetica Neue',Helvetica,sans-serif;""><table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#F7F9FA;padding:32px 16px;""><tr><td align=""center""><table role=""presentation"" width=""480"" cellpadding=""0"" cellspacing=""0"" style=""max-width:480px;width:100%;background-color:#ffffff;border-radius:12px;overflow:hidden;""><tr><td style=""background:linear-gradient(160deg,#0F6E8C 0%,#0A3040 100%);padding:28px 32px;text-align:center;""><img src=""{{logoUrl}}"" width=""28"" height=""28"" alt=""SpeedClaim"" style=""vertical-align:middle;margin-right:8px;border-radius:6px;"" /><span style=""font-size:20px;font-weight:700;color:#ffffff;letter-spacing:-0.3px;vertical-align:middle;"">SpeedClaim</span></td></tr><tr><td style=""padding:32px;""><h1 style=""margin:0 0 16px;font-size:22px;font-weight:600;color:#1A2230;"">Reset your password</h1><p style=""margin:0 0 28px;font-size:15px;line-height:1.6;color:#3C4654;"">We received a request to reset your password. Click the button below to choose a new one.</p><table role=""presentation"" cellpadding=""0"" cellspacing=""0"" width=""100%""><tr><td align=""center""><table role=""presentation"" cellpadding=""0"" cellspacing=""0""><tr><td style=""background-color:#0F6E8C;border-radius:8px;""><a href=""{{resetUrl}}"" target=""_blank"" style=""display:inline-block;padding:14px 36px;font-size:14px;font-weight:600;color:#ffffff;text-decoration:none;"">Reset Password</a></td></tr></table></td></tr></table><p style=""margin:28px 0 0;font-size:13px;line-height:1.5;color:#6B7685;"">This link expires in 1 hour. If you didn't request a password reset, you can safely ignore this email.</p></td></tr><tr><td style=""padding:20px 32px;border-top:1px solid #E2E6EB;text-align:center;""><p style=""margin:0;font-size:12px;color:#6B7685;"">&copy; {{year}} SpeedClaim. All rights reserved.</p></td></tr></table></td></tr></table></body></html>",
                    IsActive = true,
                    CreatedAt = new DateTimeOffset(2026, 6, 7, 0, 0, 0, TimeSpan.Zero)
                },
                new EmailTemplate
                {
                    Id = Guid.Parse("DD000003-0000-0000-0000-000000000003"),
                    TemplateKey = "PolicyActivated",
                    Subject = "Your SpeedClaim policy is active - {{policyNumber}}",
                    BodyHtml = @"<!DOCTYPE html><html lang=""en""><head><meta charset=""UTF-8""><meta name=""viewport"" content=""width=device-width, initial-scale=1.0""><title>Policy Activated</title></head><body style=""margin:0;padding:0;background-color:#F4F7FA;font-family:Arial,Helvetica,sans-serif;color:#1A2230;""><table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#F4F7FA;padding:28px 12px;""><tr><td align=""center""><table role=""presentation"" width=""640"" cellpadding=""0"" cellspacing=""0"" style=""max-width:640px;width:100%;background:#ffffff;border:1px solid #E2E6EB;border-radius:14px;overflow:hidden;""><tr><td style=""background:#0F6E8C;padding:24px 30px;""><table role=""presentation"" cellpadding=""0"" cellspacing=""0""><tr><td style=""vertical-align:middle;padding-right:8px;""><img src=""{{logoUrl}}"" width=""30"" height=""30"" alt=""SpeedClaim"" style=""display:block;border-radius:6px;"" /></td><td style=""vertical-align:middle;""><div style=""font-size:22px;font-weight:700;color:#ffffff;"">SpeedClaim</div></td></tr></table><div style=""font-size:13px;color:#D8EEF5;margin-top:4px;"">Insurance policy services</div></td></tr><tr><td style=""padding:30px;""><p style=""margin:0 0 10px;font-size:14px;color:#1F9D6B;font-weight:700;"">Policy activated</p><h1 style=""margin:0 0 14px;font-size:25px;line-height:1.25;color:#1A2230;"">Your cover is now active</h1><p style=""margin:0 0 24px;font-size:15px;line-height:1.65;color:#3C4654;"">Dear {{firstName}}, your policy has been activated successfully. A PDF copy of your policy certificate is attached to this email for your records.</p><table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""border-collapse:collapse;border:1px solid #E2E6EB;border-radius:10px;overflow:hidden;""><tr><td style=""padding:13px 16px;background:#F7F9FA;font-size:12px;color:#6B7685;width:38%;"">Policy number</td><td style=""padding:13px 16px;font-size:14px;font-weight:700;color:#1A2230;"">{{policyNumber}}</td></tr><tr><td style=""padding:13px 16px;background:#F7F9FA;font-size:12px;color:#6B7685;"">Product</td><td style=""padding:13px 16px;font-size:14px;color:#1A2230;"">{{product}}</td></tr><tr><td style=""padding:13px 16px;background:#F7F9FA;font-size:12px;color:#6B7685;"">Coverage amount</td><td style=""padding:13px 16px;font-size:14px;color:#1A2230;"">{{sumAssured}} INR</td></tr><tr><td style=""padding:13px 16px;background:#F7F9FA;font-size:12px;color:#6B7685;"">Premium</td><td style=""padding:13px 16px;font-size:14px;color:#1A2230;"">{{premiumAmount}} INR / {{frequency}}</td></tr><tr><td style=""padding:13px 16px;background:#F7F9FA;font-size:12px;color:#6B7685;"">Policy period</td><td style=""padding:13px 16px;font-size:14px;color:#1A2230;"">{{startDate}} - {{endDate}}</td></tr><tr><td style=""padding:13px 16px;background:#F7F9FA;font-size:12px;color:#6B7685;"">Status</td><td style=""padding:13px 16px;font-size:14px;font-weight:700;color:#1F9D6B;"">{{status}}</td></tr></table><p style=""margin:24px 0 0;font-size:13px;line-height:1.55;color:#6B7685;"">Please review the attached certificate and keep it with your insurance records. Contact SpeedClaim support if any detail looks incorrect.</p></td></tr><tr><td style=""padding:18px 30px;background:#F7F9FA;border-top:1px solid #E2E6EB;""><p style=""margin:0;font-size:12px;line-height:1.5;color:#6B7685;"">This is a system-generated message from SpeedClaim. &copy; {{year}} SpeedClaim.</p></td></tr></table></td></tr></table></body></html>",
                    IsActive = true,
                    CreatedAt = new DateTimeOffset(2026, 6, 7, 0, 0, 0, TimeSpan.Zero)
                },
                new EmailTemplate
                {
                    Id = Guid.Parse("DD000004-0000-0000-0000-000000000004"),
                    TemplateKey = "KycApproved",
                    Subject = "Your KYC has been verified - SpeedClaim",
                    BodyHtml = @"<!DOCTYPE html><html lang=""en""><head><meta charset=""UTF-8""><meta name=""viewport"" content=""width=device-width, initial-scale=1.0""><title>KYC Verified</title></head><body style=""margin:0;padding:0;background-color:#F7F9FA;font-family:Arial,'Helvetica Neue',Helvetica,sans-serif;""><table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#F7F9FA;padding:32px 16px;""><tr><td align=""center""><table role=""presentation"" width=""480"" cellpadding=""0"" cellspacing=""0"" style=""max-width:480px;width:100%;background-color:#ffffff;border-radius:12px;overflow:hidden;""><tr><td style=""background:linear-gradient(160deg,#0F6E8C 0%,#0A3040 100%);padding:28px 32px;text-align:center;""><img src=""{{logoUrl}}"" width=""28"" height=""28"" alt=""SpeedClaim"" style=""vertical-align:middle;margin-right:8px;border-radius:6px;"" /><span style=""font-size:20px;font-weight:700;color:#ffffff;letter-spacing:-0.3px;vertical-align:middle;"">SpeedClaim</span></td></tr><tr><td style=""padding:32px;""><span style=""display:inline-block;margin:0 0 12px;padding:4px 10px;background:#E8F7F1;color:#1F9D6B;border-radius:20px;font-size:11px;font-weight:700;"">KYC Verified</span><h1 style=""margin:0 0 16px;font-size:22px;font-weight:600;color:#1A2230;"">Your identity has been verified</h1><p style=""margin:0;font-size:15px;line-height:1.6;color:#3C4654;"">Dear {{firstName}}, your KYC documents have been successfully reviewed and verified. You are now eligible to apply for insurance products and have policies issued.</p></td></tr><tr><td style=""padding:20px 32px;border-top:1px solid #E2E6EB;text-align:center;""><p style=""margin:0;font-size:12px;color:#6B7685;"">&copy; {{year}} SpeedClaim. All rights reserved.</p></td></tr></table></td></tr></table></body></html>",
                    IsActive = true,
                    CreatedAt = new DateTimeOffset(2026, 6, 7, 0, 0, 0, TimeSpan.Zero)
                },
                new EmailTemplate
                {
                    Id = Guid.Parse("DD000005-0000-0000-0000-000000000005"),
                    TemplateKey = "KycRejected",
                    Subject = "KYC verification unsuccessful - SpeedClaim",
                    BodyHtml = @"<!DOCTYPE html><html lang=""en""><head><meta charset=""UTF-8""><meta name=""viewport"" content=""width=device-width, initial-scale=1.0""><title>KYC Rejected</title></head><body style=""margin:0;padding:0;background-color:#F7F9FA;font-family:Arial,'Helvetica Neue',Helvetica,sans-serif;""><table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#F7F9FA;padding:32px 16px;""><tr><td align=""center""><table role=""presentation"" width=""480"" cellpadding=""0"" cellspacing=""0"" style=""max-width:480px;width:100%;background-color:#ffffff;border-radius:12px;overflow:hidden;""><tr><td style=""background:linear-gradient(160deg,#0F6E8C 0%,#0A3040 100%);padding:28px 32px;text-align:center;""><img src=""{{logoUrl}}"" width=""28"" height=""28"" alt=""SpeedClaim"" style=""vertical-align:middle;margin-right:8px;border-radius:6px;"" /><span style=""font-size:20px;font-weight:700;color:#ffffff;letter-spacing:-0.3px;vertical-align:middle;"">SpeedClaim</span></td></tr><tr><td style=""padding:32px;""><span style=""display:inline-block;margin:0 0 12px;padding:4px 10px;background:#FBE9E9;color:#D14343;border-radius:20px;font-size:11px;font-weight:700;"">KYC Rejected</span><h1 style=""margin:0 0 16px;font-size:22px;font-weight:600;color:#1A2230;"">KYC verification unsuccessful</h1><p style=""margin:0 0 16px;font-size:15px;line-height:1.6;color:#3C4654;"">Dear {{firstName}}, we were unable to verify your identity documents.</p><div style=""margin:0 0 20px;padding:12px 16px;background:#FBE9E9;border-left:3px solid #D14343;border-radius:4px;""><p style=""margin:0 0 4px;font-size:12px;font-weight:700;color:#D14343;"">Reason</p><p style=""margin:0;font-size:14px;color:#3C4654;"">{{rejectionReason}}</p></div><p style=""margin:0;font-size:13px;line-height:1.5;color:#6B7685;"">Please log in to the customer portal to re-upload your documents.</p></td></tr><tr><td style=""padding:20px 32px;border-top:1px solid #E2E6EB;text-align:center;""><p style=""margin:0;font-size:12px;color:#6B7685;"">&copy; {{year}} SpeedClaim. All rights reserved.</p></td></tr></table></td></tr></table></body></html>",
                    IsActive = true,
                    CreatedAt = new DateTimeOffset(2026, 6, 7, 0, 0, 0, TimeSpan.Zero)
                },
                new EmailTemplate
                {
                    Id = Guid.Parse("DD000006-0000-0000-0000-000000000006"),
                    TemplateKey = "ProposalApproved",
                    Subject = "Your proposal {{proposalNumber}} has been approved",
                    BodyHtml = @"<!DOCTYPE html><html lang=""en""><head><meta charset=""UTF-8""><meta name=""viewport"" content=""width=device-width, initial-scale=1.0""><title>Proposal Approved</title></head><body style=""margin:0;padding:0;background-color:#F7F9FA;font-family:Arial,'Helvetica Neue',Helvetica,sans-serif;""><table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#F7F9FA;padding:32px 16px;""><tr><td align=""center""><table role=""presentation"" width=""480"" cellpadding=""0"" cellspacing=""0"" style=""max-width:480px;width:100%;background-color:#ffffff;border-radius:12px;overflow:hidden;""><tr><td style=""background:linear-gradient(160deg,#0F6E8C 0%,#0A3040 100%);padding:28px 32px;text-align:center;""><img src=""{{logoUrl}}"" width=""28"" height=""28"" alt=""SpeedClaim"" style=""vertical-align:middle;margin-right:8px;border-radius:6px;"" /><span style=""font-size:20px;font-weight:700;color:#ffffff;letter-spacing:-0.3px;vertical-align:middle;"">SpeedClaim</span></td></tr><tr><td style=""padding:32px;""><span style=""display:inline-block;margin:0 0 12px;padding:4px 10px;background:#E8F7F1;color:#1F9D6B;border-radius:20px;font-size:11px;font-weight:700;"">Proposal Approved</span><h1 style=""margin:0 0 16px;font-size:22px;font-weight:600;color:#1A2230;"">Your proposal has been approved</h1><p style=""margin:0;font-size:15px;line-height:1.6;color:#3C4654;"">Dear {{firstName}}, your insurance proposal <strong>{{proposalNumber}}</strong> has been reviewed and approved by our underwriting team. A policy has been created and will activate once your first premium payment is received. Please log in to complete your payment.</p></td></tr><tr><td style=""padding:20px 32px;border-top:1px solid #E2E6EB;text-align:center;""><p style=""margin:0;font-size:12px;color:#6B7685;"">&copy; {{year}} SpeedClaim. All rights reserved.</p></td></tr></table></td></tr></table></body></html>",
                    IsActive = true,
                    CreatedAt = new DateTimeOffset(2026, 6, 7, 0, 0, 0, TimeSpan.Zero)
                },
                new EmailTemplate
                {
                    Id = Guid.Parse("DD000007-0000-0000-0000-000000000007"),
                    TemplateKey = "ProposalRejected",
                    Subject = "Your proposal {{proposalNumber}} could not be approved",
                    BodyHtml = @"<!DOCTYPE html><html lang=""en""><head><meta charset=""UTF-8""><meta name=""viewport"" content=""width=device-width, initial-scale=1.0""><title>Proposal Rejected</title></head><body style=""margin:0;padding:0;background-color:#F7F9FA;font-family:Arial,'Helvetica Neue',Helvetica,sans-serif;""><table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#F7F9FA;padding:32px 16px;""><tr><td align=""center""><table role=""presentation"" width=""480"" cellpadding=""0"" cellspacing=""0"" style=""max-width:480px;width:100%;background-color:#ffffff;border-radius:12px;overflow:hidden;""><tr><td style=""background:linear-gradient(160deg,#0F6E8C 0%,#0A3040 100%);padding:28px 32px;text-align:center;""><img src=""{{logoUrl}}"" width=""28"" height=""28"" alt=""SpeedClaim"" style=""vertical-align:middle;margin-right:8px;border-radius:6px;"" /><span style=""font-size:20px;font-weight:700;color:#ffffff;letter-spacing:-0.3px;vertical-align:middle;"">SpeedClaim</span></td></tr><tr><td style=""padding:32px;""><span style=""display:inline-block;margin:0 0 12px;padding:4px 10px;background:#FEF6E6;color:#D9920A;border-radius:20px;font-size:11px;font-weight:700;"">Proposal Rejected</span><h1 style=""margin:0 0 16px;font-size:22px;font-weight:600;color:#1A2230;"">Your proposal could not be approved</h1><p style=""margin:0 0 16px;font-size:15px;line-height:1.6;color:#3C4654;"">Dear {{firstName}}, after careful review, your proposal <strong>{{proposalNumber}}</strong> has not been approved.</p><div style=""margin:0 0 20px;padding:12px 16px;background:#FEF6E6;border-left:3px solid #D9920A;border-radius:4px;""><p style=""margin:0 0 4px;font-size:12px;font-weight:700;color:#D9920A;"">Reason</p><p style=""margin:0;font-size:14px;color:#3C4654;"">{{rejectionReason}}</p></div><p style=""margin:0;font-size:13px;line-height:1.5;color:#6B7685;"">You may contact your assigned agent or our support team for more information.</p></td></tr><tr><td style=""padding:20px 32px;border-top:1px solid #E2E6EB;text-align:center;""><p style=""margin:0;font-size:12px;color:#6B7685;"">&copy; {{year}} SpeedClaim. All rights reserved.</p></td></tr></table></td></tr></table></body></html>",
                    IsActive = true,
                    CreatedAt = new DateTimeOffset(2026, 6, 7, 0, 0, 0, TimeSpan.Zero)
                },
                new EmailTemplate
                {
                    Id = Guid.Parse("DD000008-0000-0000-0000-000000000008"),
                    TemplateKey = "ClaimApproved",
                    Subject = "Your claim {{claimNumber}} has been approved",
                    BodyHtml = @"<!DOCTYPE html><html lang=""en""><head><meta charset=""UTF-8""><meta name=""viewport"" content=""width=device-width, initial-scale=1.0""><title>Claim Approved</title></head><body style=""margin:0;padding:0;background-color:#F7F9FA;font-family:Arial,'Helvetica Neue',Helvetica,sans-serif;""><table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#F7F9FA;padding:32px 16px;""><tr><td align=""center""><table role=""presentation"" width=""480"" cellpadding=""0"" cellspacing=""0"" style=""max-width:480px;width:100%;background-color:#ffffff;border-radius:12px;overflow:hidden;""><tr><td style=""background:linear-gradient(160deg,#0F6E8C 0%,#0A3040 100%);padding:28px 32px;text-align:center;""><img src=""{{logoUrl}}"" width=""28"" height=""28"" alt=""SpeedClaim"" style=""vertical-align:middle;margin-right:8px;border-radius:6px;"" /><span style=""font-size:20px;font-weight:700;color:#ffffff;letter-spacing:-0.3px;vertical-align:middle;"">SpeedClaim</span></td></tr><tr><td style=""padding:32px;""><span style=""display:inline-block;margin:0 0 12px;padding:4px 10px;background:#E8F7F1;color:#1F9D6B;border-radius:20px;font-size:11px;font-weight:700;"">Claim Approved</span><h1 style=""margin:0 0 16px;font-size:22px;font-weight:600;color:#1A2230;"">Your claim has been approved</h1><p style=""margin:0;font-size:15px;line-height:1.6;color:#3C4654;"">Dear {{firstName}}, your claim <strong>{{claimNumber}}</strong> has been approved following our investigation. Your payout will be processed shortly. You will receive a separate notification once the payment has been disbursed.</p></td></tr><tr><td style=""padding:20px 32px;border-top:1px solid #E2E6EB;text-align:center;""><p style=""margin:0;font-size:12px;color:#6B7685;"">&copy; {{year}} SpeedClaim. All rights reserved.</p></td></tr></table></td></tr></table></body></html>",
                    IsActive = true,
                    CreatedAt = new DateTimeOffset(2026, 6, 7, 0, 0, 0, TimeSpan.Zero)
                },
                new EmailTemplate
                {
                    Id = Guid.Parse("DD000009-0000-0000-0000-000000000009"),
                    TemplateKey = "ClaimRejected",
                    Subject = "Your claim {{claimNumber}} could not be approved",
                    BodyHtml = @"<!DOCTYPE html><html lang=""en""><head><meta charset=""UTF-8""><meta name=""viewport"" content=""width=device-width, initial-scale=1.0""><title>Claim Rejected</title></head><body style=""margin:0;padding:0;background-color:#F7F9FA;font-family:Arial,'Helvetica Neue',Helvetica,sans-serif;""><table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#F7F9FA;padding:32px 16px;""><tr><td align=""center""><table role=""presentation"" width=""480"" cellpadding=""0"" cellspacing=""0"" style=""max-width:480px;width:100%;background-color:#ffffff;border-radius:12px;overflow:hidden;""><tr><td style=""background:linear-gradient(160deg,#0F6E8C 0%,#0A3040 100%);padding:28px 32px;text-align:center;""><img src=""{{logoUrl}}"" width=""28"" height=""28"" alt=""SpeedClaim"" style=""vertical-align:middle;margin-right:8px;border-radius:6px;"" /><span style=""font-size:20px;font-weight:700;color:#ffffff;letter-spacing:-0.3px;vertical-align:middle;"">SpeedClaim</span></td></tr><tr><td style=""padding:32px;""><span style=""display:inline-block;margin:0 0 12px;padding:4px 10px;background:#FBE9E9;color:#D14343;border-radius:20px;font-size:11px;font-weight:700;"">Claim Rejected</span><h1 style=""margin:0 0 16px;font-size:22px;font-weight:600;color:#1A2230;"">Your claim could not be approved</h1><p style=""margin:0 0 16px;font-size:15px;line-height:1.6;color:#3C4654;"">Dear {{firstName}}, after investigation, your claim <strong>{{claimNumber}}</strong> has not been approved.</p><div style=""margin:0 0 20px;padding:12px 16px;background:#FBE9E9;border-left:3px solid #D14343;border-radius:4px;""><p style=""margin:0 0 4px;font-size:12px;font-weight:700;color:#D14343;"">Reason</p><p style=""margin:0;font-size:14px;color:#3C4654;"">{{rejectionReason}}</p></div><p style=""margin:0;font-size:13px;line-height:1.5;color:#6B7685;"">You may contact us to appeal this decision or submit additional evidence.</p></td></tr><tr><td style=""padding:20px 32px;border-top:1px solid #E2E6EB;text-align:center;""><p style=""margin:0;font-size:12px;color:#6B7685;"">&copy; {{year}} SpeedClaim. All rights reserved.</p></td></tr></table></td></tr></table></body></html>",
                    IsActive = true,
                    CreatedAt = new DateTimeOffset(2026, 6, 7, 0, 0, 0, TimeSpan.Zero)
                },
                new EmailTemplate
                {
                    Id = Guid.Parse("DD000010-0000-0000-0000-000000000010"),
                    TemplateKey = "ClaimSettled",
                    Subject = "Your claim {{claimNumber}} has been settled",
                    BodyHtml = @"<!DOCTYPE html><html lang=""en""><head><meta charset=""UTF-8""><meta name=""viewport"" content=""width=device-width, initial-scale=1.0""><title>Claim Settled</title></head><body style=""margin:0;padding:0;background-color:#F7F9FA;font-family:Arial,'Helvetica Neue',Helvetica,sans-serif;""><table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#F7F9FA;padding:32px 16px;""><tr><td align=""center""><table role=""presentation"" width=""480"" cellpadding=""0"" cellspacing=""0"" style=""max-width:480px;width:100%;background-color:#ffffff;border-radius:12px;overflow:hidden;""><tr><td style=""background:linear-gradient(160deg,#0F6E8C 0%,#0A3040 100%);padding:28px 32px;text-align:center;""><img src=""{{logoUrl}}"" width=""28"" height=""28"" alt=""SpeedClaim"" style=""vertical-align:middle;margin-right:8px;border-radius:6px;"" /><span style=""font-size:20px;font-weight:700;color:#ffffff;letter-spacing:-0.3px;vertical-align:middle;"">SpeedClaim</span></td></tr><tr><td style=""padding:32px;""><span style=""display:inline-block;margin:0 0 12px;padding:4px 10px;background:#E8F7F1;color:#1F9D6B;border-radius:20px;font-size:11px;font-weight:700;"">Claim Settled</span><h1 style=""margin:0 0 16px;font-size:22px;font-weight:600;color:#1A2230;"">Your claim payout has been processed</h1><p style=""margin:0 0 20px;font-size:15px;line-height:1.6;color:#3C4654;"">Dear {{firstName}}, your claim <strong>{{claimNumber}}</strong> has been financially settled.</p><table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""border-collapse:collapse;border:1px solid #E2E6EB;border-radius:8px;overflow:hidden;""><tr><td style=""padding:12px 16px;background:#F7F9FA;font-size:12px;color:#6B7685;width:40%;"">Claim number</td><td style=""padding:12px 16px;font-size:14px;font-weight:700;color:#1A2230;"">{{claimNumber}}</td></tr><tr><td style=""padding:12px 16px;background:#F7F9FA;font-size:12px;color:#6B7685;"">Payout amount</td><td style=""padding:12px 16px;font-size:14px;font-weight:700;color:#1F9D6B;"">{{payoutAmount}} INR</td></tr></table></td></tr><tr><td style=""padding:20px 32px;border-top:1px solid #E2E6EB;text-align:center;""><p style=""margin:0;font-size:12px;color:#6B7685;"">&copy; {{year}} SpeedClaim. All rights reserved.</p></td></tr></table></td></tr></table></body></html>",
                    IsActive = true,
                    CreatedAt = new DateTimeOffset(2026, 6, 7, 0, 0, 0, TimeSpan.Zero)
                },
                new EmailTemplate
                {
                    Id = Guid.Parse("DD000011-0000-0000-0000-000000000011"),
                    TemplateKey = "PolicyCancelled",
                    Subject = "Your policy {{policyNumber}} has been cancelled",
                    BodyHtml = @"<!DOCTYPE html><html lang=""en""><head><meta charset=""UTF-8""><meta name=""viewport"" content=""width=device-width, initial-scale=1.0""><title>Policy Cancelled</title></head><body style=""margin:0;padding:0;background-color:#F7F9FA;font-family:Arial,'Helvetica Neue',Helvetica,sans-serif;""><table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#F7F9FA;padding:32px 16px;""><tr><td align=""center""><table role=""presentation"" width=""480"" cellpadding=""0"" cellspacing=""0"" style=""max-width:480px;width:100%;background-color:#ffffff;border-radius:12px;overflow:hidden;""><tr><td style=""background:linear-gradient(160deg,#0F6E8C 0%,#0A3040 100%);padding:28px 32px;text-align:center;""><img src=""{{logoUrl}}"" width=""28"" height=""28"" alt=""SpeedClaim"" style=""vertical-align:middle;margin-right:8px;border-radius:6px;"" /><span style=""font-size:20px;font-weight:700;color:#ffffff;letter-spacing:-0.3px;vertical-align:middle;"">SpeedClaim</span></td></tr><tr><td style=""padding:32px;""><span style=""display:inline-block;margin:0 0 12px;padding:4px 10px;background:#FEF6E6;color:#D9920A;border-radius:20px;font-size:11px;font-weight:700;"">Policy Cancelled</span><h1 style=""margin:0 0 16px;font-size:22px;font-weight:600;color:#1A2230;"">Your policy has been cancelled</h1><p style=""margin:0 0 16px;font-size:15px;line-height:1.6;color:#3C4654;"">Dear {{firstName}}, your policy <strong>{{policyNumber}}</strong> has been cancelled as requested.</p><p style=""margin:0;font-size:13px;line-height:1.5;color:#6B7685;"">If you did not request this cancellation, please contact SpeedClaim support immediately.</p></td></tr><tr><td style=""padding:20px 32px;border-top:1px solid #E2E6EB;text-align:center;""><p style=""margin:0;font-size:12px;color:#6B7685;"">&copy; {{year}} SpeedClaim. All rights reserved.</p></td></tr></table></td></tr></table></body></html>",
                    IsActive = true,
                    CreatedAt = new DateTimeOffset(2026, 6, 7, 0, 0, 0, TimeSpan.Zero)
                },
                new EmailTemplate
                {
                    Id = Guid.Parse("DD000012-0000-0000-0000-000000000012"),
                    TemplateKey = "ClaimIntimated",
                    Subject = "Claim {{claimNumber}} filed — we have received your request",
                    BodyHtml = @"<!DOCTYPE html><html lang=""en""><head><meta charset=""UTF-8""><meta name=""viewport"" content=""width=device-width, initial-scale=1.0""><title>Claim Filed</title></head><body style=""margin:0;padding:0;background-color:#F7F9FA;font-family:Arial,'Helvetica Neue',Helvetica,sans-serif;""><table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#F7F9FA;padding:32px 16px;""><tr><td align=""center""><table role=""presentation"" width=""480"" cellpadding=""0"" cellspacing=""0"" style=""max-width:480px;width:100%;background-color:#ffffff;border-radius:12px;overflow:hidden;""><tr><td style=""background:linear-gradient(160deg,#0F6E8C 0%,#0A3040 100%);padding:28px 32px;text-align:center;""><img src=""{{logoUrl}}"" width=""28"" height=""28"" alt=""SpeedClaim"" style=""vertical-align:middle;margin-right:8px;border-radius:6px;"" /><span style=""font-size:20px;font-weight:700;color:#ffffff;letter-spacing:-0.3px;vertical-align:middle;"">SpeedClaim</span></td></tr><tr><td style=""padding:32px;""><span style=""display:inline-block;margin:0 0 12px;padding:4px 10px;background:#E6F4F8;color:#0F6E8C;border-radius:20px;font-size:11px;font-weight:700;"">Claim Received</span><h1 style=""margin:0 0 16px;font-size:22px;font-weight:600;color:#1A2230;"">Your claim has been filed</h1><p style=""margin:0 0 20px;font-size:15px;line-height:1.6;color:#3C4654;"">Dear {{firstName}}, your claim has been successfully filed and assigned to our team for review.</p><table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""border-collapse:collapse;border:1px solid #E2E6EB;border-radius:8px;overflow:hidden;""><tr><td style=""padding:12px 16px;background:#F7F9FA;font-size:12px;color:#6B7685;width:40%;"">Claim number</td><td style=""padding:12px 16px;font-size:14px;font-weight:700;color:#1A2230;"">{{claimNumber}}</td></tr><tr><td style=""padding:12px 16px;background:#F7F9FA;font-size:12px;color:#6B7685;"">Policy number</td><td style=""padding:12px 16px;font-size:14px;color:#1A2230;"">{{policyNumber}}</td></tr></table><p style=""margin:20px 0 0;font-size:13px;line-height:1.5;color:#6B7685;"">You will be notified as the status of your claim is updated. You can track your claim in real time via the customer portal.</p></td></tr><tr><td style=""padding:20px 32px;border-top:1px solid #E2E6EB;text-align:center;""><p style=""margin:0;font-size:12px;color:#6B7685;"">&copy; {{year}} SpeedClaim. All rights reserved.</p></td></tr></table></td></tr></table></body></html>",
                    IsActive = true,
                    CreatedAt = new DateTimeOffset(2026, 6, 7, 0, 0, 0, TimeSpan.Zero)
                },
                new EmailTemplate
                {
                    Id = Guid.Parse("DD000013-0000-0000-0000-000000000013"),
                    TemplateKey = "EndorsementApproved",
                    Subject = "Endorsement approved for policy {{policyNumber}}",
                    BodyHtml = @"<!DOCTYPE html><html lang=""en""><head><meta charset=""UTF-8""><meta name=""viewport"" content=""width=device-width, initial-scale=1.0""><title>Endorsement Approved</title></head><body style=""margin:0;padding:0;background-color:#F7F9FA;font-family:Arial,'Helvetica Neue',Helvetica,sans-serif;""><table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#F7F9FA;padding:32px 16px;""><tr><td align=""center""><table role=""presentation"" width=""480"" cellpadding=""0"" cellspacing=""0"" style=""max-width:480px;width:100%;background-color:#ffffff;border-radius:12px;overflow:hidden;""><tr><td style=""background:linear-gradient(160deg,#0F6E8C 0%,#0A3040 100%);padding:28px 32px;text-align:center;""><img src=""{{logoUrl}}"" width=""28"" height=""28"" alt=""SpeedClaim"" style=""vertical-align:middle;margin-right:8px;border-radius:6px;"" /><span style=""font-size:20px;font-weight:700;color:#ffffff;letter-spacing:-0.3px;vertical-align:middle;"">SpeedClaim</span></td></tr><tr><td style=""padding:32px;""><span style=""display:inline-block;margin:0 0 12px;padding:4px 10px;background:#E8F7F1;color:#1F9D6B;border-radius:20px;font-size:11px;font-weight:700;"">Endorsement Approved</span><h1 style=""margin:0 0 16px;font-size:22px;font-weight:600;color:#1A2230;"">Your policy change has been approved</h1><p style=""margin:0 0 20px;font-size:15px;line-height:1.6;color:#3C4654;"">Dear {{firstName}}, your endorsement request of type <strong>{{endorsementType}}</strong> for policy <strong>{{policyNumber}}</strong> has been approved and applied.</p><p style=""margin:0;font-size:13px;line-height:1.5;color:#6B7685;"">The changes are now effective on your policy. Please log in to review the updated policy details.</p></td></tr><tr><td style=""padding:20px 32px;border-top:1px solid #E2E6EB;text-align:center;""><p style=""margin:0;font-size:12px;color:#6B7685;"">&copy; {{year}} SpeedClaim. All rights reserved.</p></td></tr></table></td></tr></table></body></html>",
                    IsActive = true,
                    CreatedAt = new DateTimeOffset(2026, 6, 7, 0, 0, 0, TimeSpan.Zero)
                },
                new EmailTemplate
                {
                    Id = Guid.Parse("DD000014-0000-0000-0000-000000000014"),
                    TemplateKey = "EndorsementRejected",
                    Subject = "Endorsement request for policy {{policyNumber}} could not be approved",
                    BodyHtml = @"<!DOCTYPE html><html lang=""en""><head><meta charset=""UTF-8""><meta name=""viewport"" content=""width=device-width, initial-scale=1.0""><title>Endorsement Rejected</title></head><body style=""margin:0;padding:0;background-color:#F7F9FA;font-family:Arial,'Helvetica Neue',Helvetica,sans-serif;""><table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#F7F9FA;padding:32px 16px;""><tr><td align=""center""><table role=""presentation"" width=""480"" cellpadding=""0"" cellspacing=""0"" style=""max-width:480px;width:100%;background-color:#ffffff;border-radius:12px;overflow:hidden;""><tr><td style=""background:linear-gradient(160deg,#0F6E8C 0%,#0A3040 100%);padding:28px 32px;text-align:center;""><img src=""{{logoUrl}}"" width=""28"" height=""28"" alt=""SpeedClaim"" style=""vertical-align:middle;margin-right:8px;border-radius:6px;"" /><span style=""font-size:20px;font-weight:700;color:#ffffff;letter-spacing:-0.3px;vertical-align:middle;"">SpeedClaim</span></td></tr><tr><td style=""padding:32px;""><span style=""display:inline-block;margin:0 0 12px;padding:4px 10px;background:#FBE9E9;color:#D14343;border-radius:20px;font-size:11px;font-weight:700;"">Endorsement Rejected</span><h1 style=""margin:0 0 16px;font-size:22px;font-weight:600;color:#1A2230;"">Your policy change could not be approved</h1><p style=""margin:0 0 16px;font-size:15px;line-height:1.6;color:#3C4654;"">Dear {{firstName}}, your endorsement request of type <strong>{{endorsementType}}</strong> for policy <strong>{{policyNumber}}</strong> has not been approved.</p><div style=""margin:0 0 20px;padding:12px 16px;background:#FBE9E9;border-left:3px solid #D14343;border-radius:4px;""><p style=""margin:0 0 4px;font-size:12px;font-weight:700;color:#D14343;"">Reason</p><p style=""margin:0;font-size:14px;color:#3C4654;"">{{rejectionReason}}</p></div><p style=""margin:0;font-size:13px;line-height:1.5;color:#6B7685;"">Please contact your agent or our support team for further assistance.</p></td></tr><tr><td style=""padding:20px 32px;border-top:1px solid #E2E6EB;text-align:center;""><p style=""margin:0;font-size:12px;color:#6B7685;"">&copy; {{year}} SpeedClaim. All rights reserved.</p></td></tr></table></td></tr></table></body></html>",
                    IsActive = true,
                    CreatedAt = new DateTimeOffset(2026, 6, 7, 0, 0, 0, TimeSpan.Zero)
                },
                new EmailTemplate
                {
                    Id = Guid.Parse("DD000015-0000-0000-0000-000000000015"),
                    TemplateKey = "GrievanceFiled",
                    Subject = "Grievance {{grievanceNumber}} received — we will get back to you",
                    BodyHtml = @"<!DOCTYPE html><html lang=""en""><head><meta charset=""UTF-8""><meta name=""viewport"" content=""width=device-width, initial-scale=1.0""><title>Grievance Filed</title></head><body style=""margin:0;padding:0;background-color:#F7F9FA;font-family:Arial,'Helvetica Neue',Helvetica,sans-serif;""><table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#F7F9FA;padding:32px 16px;""><tr><td align=""center""><table role=""presentation"" width=""480"" cellpadding=""0"" cellspacing=""0"" style=""max-width:480px;width:100%;background-color:#ffffff;border-radius:12px;overflow:hidden;""><tr><td style=""background:linear-gradient(160deg,#0F6E8C 0%,#0A3040 100%);padding:28px 32px;text-align:center;""><img src=""{{logoUrl}}"" width=""28"" height=""28"" alt=""SpeedClaim"" style=""vertical-align:middle;margin-right:8px;border-radius:6px;"" /><span style=""font-size:20px;font-weight:700;color:#ffffff;letter-spacing:-0.3px;vertical-align:middle;"">SpeedClaim</span></td></tr><tr><td style=""padding:32px;""><span style=""display:inline-block;margin:0 0 12px;padding:4px 10px;background:#E6F4F8;color:#0F6E8C;border-radius:20px;font-size:11px;font-weight:700;"">Grievance Received</span><h1 style=""margin:0 0 16px;font-size:22px;font-weight:600;color:#1A2230;"">Your grievance has been registered</h1><p style=""margin:0 0 20px;font-size:15px;line-height:1.6;color:#3C4654;"">Dear {{firstName}}, we have received your grievance and assigned it reference number <strong>{{grievanceNumber}}</strong>. Our team will review it and get back to you within 7 business days.</p><p style=""margin:0;font-size:13px;line-height:1.5;color:#6B7685;"">You can track the status of your grievance at any time via the customer portal.</p></td></tr><tr><td style=""padding:20px 32px;border-top:1px solid #E2E6EB;text-align:center;""><p style=""margin:0;font-size:12px;color:#6B7685;"">&copy; {{year}} SpeedClaim. All rights reserved.</p></td></tr></table></td></tr></table></body></html>",
                    IsActive = true,
                    CreatedAt = new DateTimeOffset(2026, 6, 7, 0, 0, 0, TimeSpan.Zero)
                },
                new EmailTemplate
                {
                    Id = Guid.Parse("DD000016-0000-0000-0000-000000000016"),
                    TemplateKey = "GrievanceResolved",
                    Subject = "Your grievance {{grievanceNumber}} has been resolved",
                    BodyHtml = @"<!DOCTYPE html><html lang=""en""><head><meta charset=""UTF-8""><meta name=""viewport"" content=""width=device-width, initial-scale=1.0""><title>Grievance Resolved</title></head><body style=""margin:0;padding:0;background-color:#F7F9FA;font-family:Arial,'Helvetica Neue',Helvetica,sans-serif;""><table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#F7F9FA;padding:32px 16px;""><tr><td align=""center""><table role=""presentation"" width=""480"" cellpadding=""0"" cellspacing=""0"" style=""max-width:480px;width:100%;background-color:#ffffff;border-radius:12px;overflow:hidden;""><tr><td style=""background:linear-gradient(160deg,#0F6E8C 0%,#0A3040 100%);padding:28px 32px;text-align:center;""><img src=""{{logoUrl}}"" width=""28"" height=""28"" alt=""SpeedClaim"" style=""vertical-align:middle;margin-right:8px;border-radius:6px;"" /><span style=""font-size:20px;font-weight:700;color:#ffffff;letter-spacing:-0.3px;vertical-align:middle;"">SpeedClaim</span></td></tr><tr><td style=""padding:32px;""><span style=""display:inline-block;margin:0 0 12px;padding:4px 10px;background:#E8F7F1;color:#1F9D6B;border-radius:20px;font-size:11px;font-weight:700;"">Grievance Resolved</span><h1 style=""margin:0 0 16px;font-size:22px;font-weight:600;color:#1A2230;"">Your grievance has been resolved</h1><p style=""margin:0 0 20px;font-size:15px;line-height:1.6;color:#3C4654;"">Dear {{firstName}}, your grievance <strong>{{grievanceNumber}}</strong> has been reviewed and resolved by our team.</p><div style=""margin:0 0 20px;padding:12px 16px;background:#F7F9FA;border-left:3px solid #0F6E8C;border-radius:4px;""><p style=""margin:0 0 4px;font-size:12px;font-weight:700;color:#0F6E8C;"">Resolution</p><p style=""margin:0;font-size:14px;color:#3C4654;"">{{resolutionNotes}}</p></div><p style=""margin:0;font-size:13px;line-height:1.5;color:#6B7685;"">If you are not satisfied with the resolution, you may escalate via the customer portal.</p></td></tr><tr><td style=""padding:20px 32px;border-top:1px solid #E2E6EB;text-align:center;""><p style=""margin:0;font-size:12px;color:#6B7685;"">&copy; {{year}} SpeedClaim. All rights reserved.</p></td></tr></table></td></tr></table></body></html>",
                    IsActive = true,
                    CreatedAt = new DateTimeOffset(2026, 6, 7, 0, 0, 0, TimeSpan.Zero)
                },
                new EmailTemplate
                {
                    Id = Guid.Parse("DD000017-0000-0000-0000-000000000017"),
                    TemplateKey = "PremiumOverdue",
                    Subject = "Action required — premium overdue for policy {{policyNumber}}",
                    BodyHtml = @"<!DOCTYPE html><html lang=""en""><head><meta charset=""UTF-8""><meta name=""viewport"" content=""width=device-width, initial-scale=1.0""><title>Premium Overdue</title></head><body style=""margin:0;padding:0;background-color:#F7F9FA;font-family:Arial,'Helvetica Neue',Helvetica,sans-serif;""><table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#F7F9FA;padding:32px 16px;""><tr><td align=""center""><table role=""presentation"" width=""480"" cellpadding=""0"" cellspacing=""0"" style=""max-width:480px;width:100%;background-color:#ffffff;border-radius:12px;overflow:hidden;""><tr><td style=""background:linear-gradient(160deg,#0F6E8C 0%,#0A3040 100%);padding:28px 32px;text-align:center;""><img src=""{{logoUrl}}"" width=""28"" height=""28"" alt=""SpeedClaim"" style=""vertical-align:middle;margin-right:8px;border-radius:6px;"" /><span style=""font-size:20px;font-weight:700;color:#ffffff;letter-spacing:-0.3px;vertical-align:middle;"">SpeedClaim</span></td></tr><tr><td style=""padding:32px;""><span style=""display:inline-block;margin:0 0 12px;padding:4px 10px;background:#FBE9E9;color:#D14343;border-radius:20px;font-size:11px;font-weight:700;"">Premium Overdue</span><h1 style=""margin:0 0 16px;font-size:22px;font-weight:600;color:#1A2230;"">Your premium payment is overdue</h1><p style=""margin:0 0 20px;font-size:15px;line-height:1.6;color:#3C4654;"">Dear {{firstName}}, a premium payment for your policy is now overdue. Please make your payment as soon as possible to avoid a lapse in coverage.</p><table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""border-collapse:collapse;border:1px solid #E2E6EB;border-radius:8px;overflow:hidden;""><tr><td style=""padding:12px 16px;background:#F7F9FA;font-size:12px;color:#6B7685;width:40%;"">Policy number</td><td style=""padding:12px 16px;font-size:14px;font-weight:700;color:#1A2230;"">{{policyNumber}}</td></tr><tr><td style=""padding:12px 16px;background:#F7F9FA;font-size:12px;color:#6B7685;"">Amount due</td><td style=""padding:12px 16px;font-size:14px;font-weight:700;color:#D14343;"">{{amount}} INR</td></tr><tr><td style=""padding:12px 16px;background:#F7F9FA;font-size:12px;color:#6B7685;"">Was due on</td><td style=""padding:12px 16px;font-size:14px;color:#1A2230;"">{{dueDate}}</td></tr></table><p style=""margin:20px 0 0;font-size:13px;line-height:1.5;color:#6B7685;"">Log in to the customer portal to make your payment immediately.</p></td></tr><tr><td style=""padding:20px 32px;border-top:1px solid #E2E6EB;text-align:center;""><p style=""margin:0;font-size:12px;color:#6B7685;"">&copy; {{year}} SpeedClaim. All rights reserved.</p></td></tr></table></td></tr></table></body></html>",
                    IsActive = true,
                    CreatedAt = new DateTimeOffset(2026, 6, 7, 0, 0, 0, TimeSpan.Zero)
                }
            );
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
            e.HasData(
                new SystemConfig { Id = Guid.Parse("CC000001-0000-0000-0000-000000000001"), ConfigKey = "AllowCustomerRegistration", ConfigValue = "true", Description = "Allow new customers to self-register via the portal. Set to false to disable public sign-up." },
                new SystemConfig { Id = Guid.Parse("CC000002-0000-0000-0000-000000000002"), ConfigKey = "PremiumGracePeriodDays", ConfigValue = "30", Description = "Number of days after the due date before a policy lapses for non-payment." },
                new SystemConfig { Id = Guid.Parse("CC000003-0000-0000-0000-000000000003"), ConfigKey = "ClaimApprovalThresholdInr", ConfigValue = "100000", Description = "Claims above this amount (INR) require senior underwriter sign-off before approval." },
                new SystemConfig { Id = Guid.Parse("CC000004-0000-0000-0000-000000000004"), ConfigKey = "KycVerificationRequired", ConfigValue = "true", Description = "Require completed KYC verification before a policy can be issued to a customer." },
                new SystemConfig { Id = Guid.Parse("CC000005-0000-0000-0000-000000000005"), ConfigKey = "MaxPremiumPaymentRetries", ConfigValue = "3", Description = "Maximum number of times a failed premium payment can be retried before the schedule is flagged." }
            );
        });

        modelBuilder.Entity<UserConsent>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_user_consents");
            e.HasIndex(x => new { x.UserId, x.ConsentType }).HasDatabaseName("ix_user_consents_user_type");
            e.Property(x => x.ConsentType).HasMaxLength(50);
            e.Property(x => x.ConsentVersion).HasMaxLength(10);
            e.Property(x => x.IpAddress).HasMaxLength(45);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProcessedWebhookEvent>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_processed_webhook_events");
            e.HasIndex(x => x.StripeEventId).IsUnique().HasDatabaseName("uq_processed_webhook_events_stripe_event_id");
            e.Property(x => x.StripeEventId).IsRequired().HasMaxLength(255);
            e.Property(x => x.EventType).IsRequired().HasMaxLength(100);
        });
    }
    
    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        var startUnderscores = System.Text.RegularExpressions.Regex.Match(input, @"^_+");
        return startUnderscores + System.Text.RegularExpressions.Regex.Replace(input, @"([a-z0-9])([A-Z])", "$1_$2").ToLower();
    }
}
