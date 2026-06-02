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
    public DbSet<PolicyHealthDetail> PolicyHealthDetails { get; set; }
    public DbSet<PolicyVehicleDetail> PolicyVehicleDetails { get; set; }
    public DbSet<PolicyLifeDetail> PolicyLifeDetails { get; set; }
    public DbSet<PolicyInsuredMember> PolicyInsuredMembers { get; set; }

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

        // Configuration for Identity
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_users");
            e.HasIndex(x => x.Email).IsUnique().HasDatabaseName("uq_users_email");
            e.Property(x => x.Email).IsRequired().HasMaxLength(255);
            e.Property(x => x.PasswordHash).IsRequired().HasMaxLength(255);
        });

        modelBuilder.Entity<Role>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_roles");
            e.HasIndex(x => x.Code).IsUnique().HasDatabaseName("uq_roles_code");
            e.Property(x => x.Code).IsRequired().HasMaxLength(50);
            e.ToTable(t => t.HasCheckConstraint("CK_roles_hierarchy_level", "hierarchy_level > 0"));
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
        });

        modelBuilder.Entity<PolicyVersion>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_policy_versions");
            e.HasIndex(x => new { x.PolicyId, x.VersionNumber }).IsUnique().HasDatabaseName("uq_policy_versions_policy_id_version_number");
            e.HasOne(x => x.Policy).WithMany(p => p.Versions).HasForeignKey(x => x.PolicyId).HasConstraintName("FK_policy_versions_policies_policy_id").OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PolicyHealthDetail>(e =>
        {
            e.HasKey(x => x.PolicyId).HasName("PK_policy_health_details");
            e.Property(x => x.NetworkType).IsRequired().HasMaxLength(20);
            e.ToTable(t => t.HasCheckConstraint("CK_health_network", "network_type IN ('TPA', 'CASHLESS', 'REIMBURSEMENT')"));
            e.HasOne(x => x.Policy).WithOne(p => p.HealthDetail).HasForeignKey<PolicyHealthDetail>(x => x.PolicyId).HasConstraintName("FK_policy_health_details_policies_policy_id").OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PolicyVehicleDetail>(e =>
        {
            e.HasKey(x => x.PolicyId).HasName("PK_policy_vehicle_details");
            e.HasIndex(x => x.VehicleNumber).IsUnique().HasDatabaseName("uq_policy_vehicle_details_vehicle_number");
            e.Property(x => x.VehicleNumber).IsRequired().HasMaxLength(30);
            e.Property(x => x.Make).IsRequired().HasMaxLength(100);
            e.Property(x => x.Model).IsRequired().HasMaxLength(100);
            e.HasOne(x => x.Policy).WithOne(p => p.VehicleDetail).HasForeignKey<PolicyVehicleDetail>(x => x.PolicyId).HasConstraintName("FK_policy_vehicle_details_policies_policy_id").OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PolicyLifeDetail>(e =>
        {
            e.HasKey(x => x.PolicyId).HasName("PK_policy_life_details");
            e.Property(x => x.NomineeName).IsRequired().HasMaxLength(200);
            e.Property(x => x.NomineeRelation).IsRequired().HasMaxLength(50);
            e.Property(x => x.NomineePhone).HasMaxLength(20);
            e.HasOne(x => x.Policy).WithOne(p => p.LifeDetail).HasForeignKey<PolicyLifeDetail>(x => x.PolicyId).HasConstraintName("FK_policy_life_details_policies_policy_id").OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PolicyInsuredMember>(e =>
        {
            e.HasKey(x => x.Id).HasName("PK_policy_insured_members");
            e.Property(x => x.FullName).IsRequired().HasMaxLength(200);
            e.Property(x => x.RelationToHolder).IsRequired().HasMaxLength(50);
            e.HasOne(x => x.Policy).WithMany(p => p.InsuredMembers).HasForeignKey(x => x.PolicyId).HasConstraintName("FK_policy_insured_members_policies_policy_id").OnDelete(DeleteBehavior.Cascade);
        });
    }
    
    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        var startUnderscores = System.Text.RegularExpressions.Regex.Match(input, @"^_+");
        return startUnderscores + System.Text.RegularExpressions.Regex.Replace(input, @"([a-z0-9])([A-Z])", "$1_$2").ToLower();
    }
}
