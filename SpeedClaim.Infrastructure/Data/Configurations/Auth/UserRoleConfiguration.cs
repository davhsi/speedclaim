using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpeedClaim.Core.Entities.Auth;

namespace SpeedClaim.Infrastructure.Data.Configurations.Auth;

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles");

        builder.HasKey(ur => ur.Id)
            .HasName("PK_user_roles");

        builder.Property(ur => ur.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(ur => ur.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(ur => ur.RoleId)
            .HasColumnName("role_id")
            .IsRequired();

        builder.Property(ur => ur.AssignedAt)
            .HasColumnName("assigned_at")
            .IsRequired();

        builder.Property(ur => ur.RevokedAt)
            .HasColumnName("revoked_at");

        builder.Property(ur => ur.AssignedBy)
            .HasColumnName("assigned_by")
            .HasMaxLength(100)
            .IsRequired();

        // Foreign keys
        builder.HasOne(ur => ur.User)
            .WithMany()
            .HasForeignKey(ur => ur.UserId)
            .HasConstraintName("FK_user_roles_users_user_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ur => ur.Role)
            .WithMany()
            .HasForeignKey(ur => ur.RoleId)
            .HasConstraintName("FK_user_roles_roles_role_id")
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraint on active user roles
        builder.HasIndex(ur => new { ur.UserId, ur.RoleId })
            .IsUnique()
            .HasFilter("revoked_at IS NULL")
            .HasDatabaseName("UQ_user_roles_user_id_role_id");
    }
}
