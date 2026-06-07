using Microsoft.EntityFrameworkCore;
using SpeedClaim.Api.Models;

namespace SpeedClaim.Api.Context;

public class SpeedClaimDbContext : DbContext
{
    public SpeedClaimDbContext(DbContextOptions<SpeedClaimDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Agent> Agents { get; set; }
    public DbSet<InsuranceProduct> InsuranceProducts { get; set; }
    public DbSet<Policy> Policies { get; set; }
    public DbSet<PolicyVersion> PolicyVersions { get; set; }
    public DbSet<HealthPolicy> HealthPolicies { get; set; }
    public DbSet<VehiclePolicy> VehiclePolicies { get; set; }
    public DbSet<LifePolicy> LifePolicies { get; set; }
    public DbSet<PolicyInsuredMember> PolicyInsuredMembers { get; set; }
    public DbSet<DocumentType> DocumentTypes { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<Claim> Claims { get; set; }
    public DbSet<ClaimWorkflow> ClaimWorkflows { get; set; } = null!;
    public DbSet<PaymentTransaction> PaymentTransactions { get; set; } = null!;
    public DbSet<ClaimHealthDetail> ClaimHealthDetails { get; set; } = null!;
    public DbSet<ClaimVehicleDetail> ClaimVehicleDetails { get; set; } = null!;
    public DbSet<ClaimLifeDetail> ClaimLifeDetails { get; set; } = null!;
    public DbSet<ClaimDocumentChecklist> ClaimDocumentChecklists { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public DbSet<UserConsent> UserConsents { get; set; } = null!;
    public DbSet<PaymentStatusHistory> PaymentStatusHistories { get; set; } = null!;
    public DbSet<PremiumSchedule> PremiumSchedules { get; set; } = null!;
    public DbSet<Address> Addresses { get; set; } = null!;
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

        // Configuration for Address
        modelBuilder.Entity<Address>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_addresses");
            e.Property(x => x.Street).IsRequired().HasMaxLength(255);
            e.Property(x => x.City).IsRequired().HasMaxLength(100);
            e.Property(x => x.State).IsRequired().HasMaxLength(100);
            e.Property(x => x.PostalCode).IsRequired().HasMaxLength(20);
            e.Property(x => x.Country).IsRequired().HasMaxLength(100);
        });

        // Configuration for Identity
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_users");
            e.HasIndex(x => x.Email).IsUnique().HasDatabaseName("uq_users_email");
            e.Property(x => x.Email).IsRequired().HasMaxLength(255);
            e.Property(x => x.PasswordHash).IsRequired().HasMaxLength(255);
            e.Property(x => x.Salutation).HasMaxLength(20);
            e.Property(x => x.FirstName).IsRequired().HasMaxLength(100);
            e.Property(x => x.LastName).IsRequired().HasMaxLength(100);
            e.Ignore(x => x.FullName);
            
            e.HasIndex(x => x.AadhaarNumber).IsUnique().HasDatabaseName("uq_users_aadhaar");
            e.Property(x => x.AadhaarNumber).IsRequired().HasMaxLength(12);
            
            e.HasIndex(x => x.PanNumber).IsUnique().HasDatabaseName("uq_users_pan");
            e.Property(x => x.PanNumber).IsRequired().HasMaxLength(10);
            
            e.Property(x => x.Gender).IsRequired().HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.MaritalStatus).IsRequired().HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.KycStatus).IsRequired().HasMaxLength(20);

            e.HasOne(x => x.Address).WithMany().HasForeignKey(x => x.AddressId).HasConstraintName("FK_users_addresses_address_id").OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Role>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_roles");
            e.HasIndex(x => x.Code).IsUnique().HasDatabaseName("uq_roles_code");
            e.Property(x => x.Code).IsRequired().HasMaxLength(50);
            e.ToTable(t => t.HasCheckConstraint("CK_roles_hierarchy_level", "hierarchy_level > 0"));
            
            e.HasData(
                new Role { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Code = "Customer", Description = "Standard customer", HierarchyLevel = 10 },
                new Role { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Code = "Agent", Description = "Insurance agent", HierarchyLevel = 20 },
                new Role { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Code = "Admin", Description = "System administrator", HierarchyLevel = 50 }
            );
        });

        modelBuilder.Entity<ClaimWorkflow>(entity =>
        {
            entity.HasKey(cw => cw.Id);
            
            entity.HasOne(cw => cw.Claim)
                  .WithMany(c => c.Workflows)
                  .HasForeignKey(cw => cw.ClaimId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(cw => cw.Actor)
                  .WithMany()
                  .HasForeignKey(cw => cw.ActorId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.HasKey(pt => pt.Id);
            
            entity.HasOne(pt => pt.Policy)
                  .WithMany()
                  .HasForeignKey(pt => pt.PolicyId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(pt => pt.StripePaymentIntentId).IsUnique();
        });

        modelBuilder.Entity<UserRole>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_user_roles");
            e.HasOne(x => x.User).WithMany(u => u.UserRoles).HasForeignKey(x => x.UserId).HasConstraintName("FK_user_roles_users_user_id").OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Role).WithMany(r => r.UserRoles).HasForeignKey(x => x.RoleId).HasConstraintName("FK_user_roles_roles_role_id").OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_refresh_tokens");
            e.HasIndex(x => x.TokenHash).IsUnique().HasDatabaseName("uq_refresh_tokens_token_hash");
            e.Property(x => x.TokenHash).IsRequired().HasMaxLength(255);
            e.HasOne(x => x.User).WithMany(u => u.RefreshTokens).HasForeignKey(x => x.UserId).HasConstraintName("FK_refresh_tokens_users_user_id").OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Agent>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_agents");
            e.HasIndex(x => x.UserId).IsUnique().HasDatabaseName("uq_agents_user_id");
            e.HasIndex(x => x.LicenseNumber).IsUnique().HasDatabaseName("uq_agents_license_number");
            e.Property(x => x.LicenseNumber).IsRequired().HasMaxLength(50);
            e.HasOne(x => x.User).WithOne(u => u.Agent).HasForeignKey<Agent>(x => x.UserId).HasConstraintName("FK_agents_users_user_id").OnDelete(DeleteBehavior.Cascade);
        });

        // Configuration for Products
        modelBuilder.Entity<InsuranceProduct>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_insurance_products");
            e.HasIndex(x => x.Code).IsUnique().HasDatabaseName("uq_insurance_products_code");
            e.HasIndex(x => new { x.Id, x.Domain }).IsUnique().HasDatabaseName("UQ_product_id_domain");
            e.Property(x => x.Code).IsRequired().HasMaxLength(50);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Domain).IsRequired().HasMaxLength(20);
        });

        // Configuration for Policies
        modelBuilder.Entity<Policy>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_policies");
            e.HasIndex(x => x.PolicyNumber).IsUnique().HasFilter("deleted_at IS NULL").HasDatabaseName("uq_policy_num");
            e.HasIndex(x => new { x.Id, x.Domain }).IsUnique().HasDatabaseName("UQ_policy_id_domain");
            e.Property(x => x.PolicyNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.Status).IsRequired().HasMaxLength(20);
            e.Property(x => x.PaymentFrequency).IsRequired().HasMaxLength(20);
            e.Property(x => x.Currency).HasMaxLength(3);
            e.Property(x => x.Domain).IsRequired().HasMaxLength(20);
            
            e.ToTable(t => t.HasCheckConstraint("CK_policies_status", "status IN ('ACTIVE', 'LAPSED', 'CANCELLED', 'EXPIRED', 'CLAIMED')"));
            e.ToTable(t => t.HasCheckConstraint("CK_policies_payment_frequency", "payment_frequency IN ('MONTHLY', 'QUARTERLY', 'ANNUAL')"));

            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).HasConstraintName("FK_policies_users_user_id");
            e.HasOne(x => x.Agent).WithMany().HasForeignKey(x => x.AgentId).HasConstraintName("FK_policies_agents_agent_id");
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).HasConstraintName("FK_policies_insurance_products_product_id");

            e.HasDiscriminator<string>("domain")
                .HasValue<HealthPolicy>("HEALTH")
                .HasValue<VehiclePolicy>("VEHICLE")
                .HasValue<LifePolicy>("LIFE");
        });

        modelBuilder.Entity<PolicyVersion>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_policy_versions");
            e.HasIndex(x => new { x.PolicyId, x.VersionNumber }).IsUnique().HasDatabaseName("uq_policy_versions_policy_id_version_number");
            e.HasOne(x => x.Policy).WithMany(p => p.Versions).HasForeignKey(x => x.PolicyId).HasConstraintName("FK_policy_versions_policies_policy_id").OnDelete(DeleteBehavior.Cascade);
        });



        modelBuilder.Entity<PolicyInsuredMember>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_policy_insured_members");
            e.Property(x => x.Salutation).HasMaxLength(20);
            e.Property(x => x.FirstName).IsRequired().HasMaxLength(100);
            e.Property(x => x.LastName).IsRequired().HasMaxLength(100);
            e.Ignore(x => x.FullName);
            e.Property(x => x.RelationToHolder).IsRequired().HasMaxLength(50);
            e.HasOne(x => x.Policy).WithMany(p => p.InsuredMembers).HasForeignKey(x => x.PolicyId).HasConstraintName("FK_policy_insured_members_policies_policy_id").OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Address).WithMany().HasForeignKey(x => x.AddressId).HasConstraintName("FK_policy_insured_members_addresses_address_id").OnDelete(DeleteBehavior.Restrict);
        });

        // Configuration for Documents
        modelBuilder.Entity<DocumentType>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_document_types");
            e.HasIndex(x => new { x.Code, x.Domain }).IsUnique().HasDatabaseName("UQ_doctype_code_domain");
            e.Property(x => x.Code).IsRequired().HasMaxLength(50);
            e.Property(x => x.Domain).IsRequired().HasMaxLength(20);
            e.Property(x => x.Name).IsRequired().HasMaxLength(150);

            e.HasData(
                new DocumentType { Id = Guid.Parse("10000000-0000-0000-0000-000000000001"), Code = "AADHAAR", Domain = "AUTH", Name = "Aadhaar Card", IsSensitivePhiPii = true },
                new DocumentType { Id = Guid.Parse("10000000-0000-0000-0000-000000000002"), Code = "PAN", Domain = "AUTH", Name = "PAN Card", IsSensitivePhiPii = true },
                new DocumentType { Id = Guid.Parse("10000000-0000-0000-0000-000000000003"), Code = "PHOTOGRAPH", Domain = "AUTH", Name = "Passport Size Photograph", IsSensitivePhiPii = false },
                new DocumentType { Id = Guid.Parse("10000000-0000-0000-0000-000000000004"), Code = "ADDRESS_PROOF", Domain = "AUTH", Name = "Proof of Address", IsSensitivePhiPii = false },
                // Generic supporting doc for all domains (using placeholder "GENERAL", though it could be HEALTH/VEHICLE/LIFE)
                new DocumentType { Id = Guid.Parse("10000000-0000-0000-0000-000000000005"), Code = "SUPPORTING_DOC", Domain = "HEALTH", Name = "Supporting Document", IsSensitivePhiPii = true },
                new DocumentType { Id = Guid.Parse("10000000-0000-0000-0000-000000000006"), Code = "SUPPORTING_DOC", Domain = "VEHICLE", Name = "Supporting Document", IsSensitivePhiPii = false },
                new DocumentType { Id = Guid.Parse("10000000-0000-0000-0000-000000000007"), Code = "SUPPORTING_DOC", Domain = "LIFE", Name = "Supporting Document", IsSensitivePhiPii = true }
            );
        });

        modelBuilder.Entity<Document>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_documents");
            e.Property(x => x.Domain).IsRequired().HasMaxLength(20);
            e.Property(x => x.DocumentTypeCode).IsRequired().HasMaxLength(50);
            e.Property(x => x.FileName).IsRequired().HasMaxLength(300);
            e.Property(x => x.VerificationStatus).IsRequired().HasMaxLength(20);
            e.ToTable(t => t.HasCheckConstraint("CK_doc_verification", "verification_status IN ('PENDING', 'VERIFIED', 'REJECTED')"));
            
            e.Property(x => x.RejectionReason).HasMaxLength(500);
            
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).HasConstraintName("FK_documents_users_user_id");
            e.HasOne(x => x.ReviewedBy).WithMany().HasForeignKey(x => x.ReviewedById).HasConstraintName("FK_documents_users_reviewed_by_id");
            // FK to Claim added below once Claims table is configured
        });

        // Configuration for Claims
        modelBuilder.Entity<Claim>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_claims");
            e.HasIndex(x => x.ClaimNumber).IsUnique().HasDatabaseName("uq_claims_claim_number");
            e.HasIndex(x => new { x.Id, x.Domain }).IsUnique().HasDatabaseName("UQ_claim_id_domain");
            e.Property(x => x.ClaimNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.Status).IsRequired().HasMaxLength(30);
            e.Property(x => x.Domain).IsRequired().HasMaxLength(20);
            e.Property(x => x.ClaimedAmount).HasColumnType("decimal(14,2)");
            e.Property(x => x.ApprovedAmount).HasColumnType("decimal(14,2)");
            e.Property(x => x.RiskScore).HasColumnType("decimal(5,2)").HasDefaultValue(0.00m);
            
            e.ToTable(t => t.HasCheckConstraint("CK_claims_status", "status IN ('SUBMITTED', 'UNDER_REVIEW', 'ESCALATED', 'APPROVED', 'REJECTED', 'SETTLED', 'CLOSED')"));
            e.ToTable(t => t.HasCheckConstraint("CK_claims_priority", "priority IN (1, 2, 3)"));

            e.HasOne(x => x.Policy).WithMany().HasForeignKey(x => x.PolicyId).HasConstraintName("FK_claims_policies_policy_id").OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.SubmittedBy).WithMany().HasForeignKey(x => x.SubmittedById).HasConstraintName("FK_claims_users_submitted_by_id");
            e.HasOne(x => x.AssignedAdjuster).WithMany().HasForeignKey(x => x.AssignedAdjusterId).HasConstraintName("FK_claims_users_assigned_adjuster_id");
        });

        // Configuration for ClaimWorkflows
        modelBuilder.Entity<ClaimWorkflow>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_claim_workflow");
            e.Property(x => x.FromStatus).HasMaxLength(30);
            e.Property(x => x.ToStatus).IsRequired().HasMaxLength(30);

            e.HasOne(x => x.Claim).WithMany(c => c.Workflows).HasForeignKey(x => x.ClaimId).HasConstraintName("FK_claim_workflow_claims_claim_id").OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Actor).WithMany().HasForeignKey(x => x.ActorId).HasConstraintName("FK_claim_workflow_users_actor_id");
        });

        // Update Document to link to Claims optionally
        modelBuilder.Entity<Document>(e =>
        {
            e.HasOne(x => x.Claim).WithMany(c => c.Documents).HasForeignKey(x => x.ClaimId).HasConstraintName("FK_documents_claims_claim_id").OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ClaimHealthDetail>(e =>
        {
            e.HasKey(x => x.ClaimId).HasName("PK_claim_health_details");
            e.Property(x => x.HospitalName).IsRequired().HasMaxLength(200);
            e.Property(x => x.TreatingDoctor).IsRequired().HasMaxLength(200);
            e.HasOne(x => x.Claim).WithOne(c => c.HealthDetail).HasForeignKey<ClaimHealthDetail>(x => x.ClaimId).HasConstraintName("FK_claim_health_details_claims_claim_id").OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.InsuredMember).WithMany(m => m.ClaimHealthDetails).HasForeignKey(x => x.InsuredMemberId).HasConstraintName("FK_claim_health_details_policy_insured_members_insured_member_id");
        });

        modelBuilder.Entity<ClaimVehicleDetail>(e =>
        {
            e.HasKey(x => x.ClaimId).HasName("PK_claim_vehicle_details");
            e.Property(x => x.FirNumber).HasMaxLength(50);
            e.Property(x => x.SurveyorName).HasMaxLength(200);
            e.Property(x => x.RepairEstimate).HasColumnType("decimal(14,2)");
            e.HasOne(x => x.Claim).WithOne(c => c.VehicleDetail).HasForeignKey<ClaimVehicleDetail>(x => x.ClaimId).HasConstraintName("FK_claim_vehicle_details_claims_claim_id").OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ClaimLifeDetail>(e =>
        {
            e.HasKey(x => x.ClaimId).HasName("PK_claim_life_details");
            e.Property(x => x.CauseOfDeath).IsRequired().HasMaxLength(255);
            e.Property(x => x.PlaceOfDeath).IsRequired().HasMaxLength(255);
            e.Property(x => x.DeathCertificateNumber).HasMaxLength(100);
            e.Property(x => x.CertifyingDoctor).IsRequired().HasMaxLength(200);
            e.Property(x => x.ClaimantName).IsRequired().HasMaxLength(200);
            e.Property(x => x.ClaimantRelation).IsRequired().HasMaxLength(50);
            e.HasOne(x => x.Claim).WithOne(c => c.LifeDetail).HasForeignKey<ClaimLifeDetail>(x => x.ClaimId).HasConstraintName("FK_claim_life_details_claims_claim_id").OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ClaimDocumentChecklist>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_claim_document_checklist");
            e.HasIndex(x => new { x.ClaimId, x.DocumentTypeCode }).IsUnique().HasDatabaseName("uq_claim_document_checklist_claim_id_document_type_code");
            e.Property(x => x.Domain).IsRequired().HasMaxLength(20);
            e.Property(x => x.DocumentTypeCode).IsRequired().HasMaxLength(50);
            e.HasOne(x => x.Claim).WithMany(c => c.DocumentChecklists).HasForeignKey(x => x.ClaimId).HasConstraintName("FK_checklist_claim").OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_audit_logs");
            e.HasIndex(x => new { x.EntityType, x.EntityId }).HasDatabaseName("idx_audit_logs_composite");
            e.Property(x => x.EntityType).IsRequired().HasMaxLength(50);
            e.Property(x => x.Action).IsRequired().HasMaxLength(50);
            e.Property(x => x.OldValues).HasColumnType("jsonb");
            e.Property(x => x.NewValues).HasColumnType("jsonb");
        });

        modelBuilder.Entity<UserConsent>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_user_consents");
            e.Property(x => x.ConsentType).IsRequired().HasMaxLength(50);
            e.Property(x => x.ConsentVersion).IsRequired().HasMaxLength(20);
            e.HasOne(x => x.User).WithMany(u => u.Consents).HasForeignKey(x => x.UserId).HasConstraintName("FK_user_consents_users_user_id").OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PaymentStatusHistory>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_payment_status_history");
            e.Property(x => x.OldStatus).HasMaxLength(20);
            e.Property(x => x.NewStatus).IsRequired().HasMaxLength(20);
            e.HasOne(x => x.Payment).WithMany(p => p.StatusHistories).HasForeignKey(x => x.PaymentId).HasConstraintName("FK_payment_status_history_payment_transactions_payment_id").OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ChangedBy).WithMany(u => u.StatusChanges).HasForeignKey(x => x.ChangedById).HasConstraintName("FK_payment_status_history_users_changed_by_id");
        });

        modelBuilder.Entity<PremiumSchedule>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_premium_schedule");
            e.Property(x => x.AmountDue).HasColumnType("decimal(12,2)");
            e.Property(x => x.Status).IsRequired().HasMaxLength(20);
            e.HasIndex(x => new { x.DueDate, x.Status }).HasDatabaseName("idx_premium_schedule_due");
            e.HasOne(x => x.Policy).WithMany(p => p.PremiumSchedules).HasForeignKey(x => x.PolicyId).HasConstraintName("FK_premium_schedule_policies_policy_id").OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Payment).WithMany(p => p.PremiumSchedules).HasForeignKey(x => x.PaymentId).HasConstraintName("FK_premium_schedule_payment_transactions_payment_id");
        });
    }
    
    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        var startUnderscores = System.Text.RegularExpressions.Regex.Match(input, @"^_+");
        return startUnderscores + System.Text.RegularExpressions.Regex.Replace(input, @"([a-z0-9])([A-Z])", "$1_$2").ToLower();
    }
}
