using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loremaster.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("user");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasMaxLength(255)
            .IsRequired();

        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("uq_user_email");

        builder.HasIndex(u => u.Email)
            .HasDatabaseName("ix_user_email")
            .HasFilter("deleted_at IS NULL");

        builder.Property(u => u.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(u => u.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(100);

        builder.Property(u => u.Role)
            .HasColumnName("role")
            .HasColumnType("user_role")
            .HasDefaultValue(UserRole.Player)
            .IsRequired();

        builder.HasIndex(u => u.Role)
            .HasDatabaseName("ix_user_role")
            .HasFilter("deleted_at IS NULL");

        builder.Property(u => u.AvatarUrl)
            .HasColumnName("avatar_url")
            .HasColumnType("text");

        // External authentication support (optional)
        builder.Property(u => u.ExternalId)
            .HasColumnName("external_id")
            .HasMaxLength(255);

        builder.HasIndex(u => u.ExternalId)
            .IsUnique()
            .HasDatabaseName("uq_user_external_id")
            .HasFilter("external_id IS NOT NULL");

        builder.HasIndex(u => u.ExternalId)
            .HasDatabaseName("ix_user_external_id")
            .HasFilter("external_id IS NOT NULL AND deleted_at IS NULL");

        // Password-based authentication fields
        builder.Property(u => u.RefreshToken)
            .HasColumnName("refresh_token")
            .HasMaxLength(500);

        builder.Property(u => u.RefreshTokenExpiryTime)
            .HasColumnName("refresh_token_expiry_time")
            .HasColumnType("timestamp with time zone");

        builder.Property(u => u.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasIndex(u => u.IsActive)
            .HasDatabaseName("ix_user_is_active")
            .HasFilter("deleted_at IS NULL");

        builder.Property(u => u.LastLoginAt)
            .HasColumnName("last_login_at")
            .HasColumnType("timestamp with time zone");

        // Master-Player relationship
        builder.Property(u => u.InvitationCode)
            .HasColumnName("invitation_code")
            .HasMaxLength(20);

        builder.HasIndex(u => u.InvitationCode)
            .IsUnique()
            .HasDatabaseName("uq_user_invitation_code")
            .HasFilter("invitation_code IS NOT NULL");

        builder.HasIndex(u => u.InvitationCode)
            .HasDatabaseName("ix_user_invitation_code")
            .HasFilter("invitation_code IS NOT NULL AND deleted_at IS NULL AND is_active = true");

        builder.Property(u => u.MasterId)
            .HasColumnName("master_id")
            .HasColumnType("uuid");

        builder.HasIndex(u => u.MasterId)
            .HasDatabaseName("ix_user_master_id")
            .HasFilter("master_id IS NOT NULL AND deleted_at IS NULL");

        // Self-referencing relationship: Master -> Players
        builder.HasOne(u => u.Master)
            .WithMany(u => u.Players)
            .HasForeignKey(u => u.MasterId)
            .HasConstraintName("fk_user_master")
            .OnDelete(DeleteBehavior.SetNull);

        // Audit fields
        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(u => u.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(u => u.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(u => u.DeletedAt)
            .HasDatabaseName("ix_user_deleted_at")
            .HasFilter("deleted_at IS NOT NULL");

        // Ignore base class properties not in schema
        builder.Ignore(u => u.CreatedBy);
        builder.Ignore(u => u.UpdatedBy);
        builder.Ignore(u => u.DomainEvents);
    }
}
