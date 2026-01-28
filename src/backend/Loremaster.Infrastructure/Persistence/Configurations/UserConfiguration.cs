using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loremaster.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Table
        builder.ToTable("users");

        // Primary Key (UUID)
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        // Email (unique)
        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("ix_users_email");

        // PasswordHash
        builder.Property(u => u.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(512)
            .IsRequired();

        // Role
        builder.Property(u => u.Role)
            .HasColumnName("role")
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(UserRole.Player)
            .IsRequired();

        builder.HasIndex(u => u.Role)
            .HasDatabaseName("ix_users_role");

        // DisplayName
        builder.Property(u => u.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(100);

        // IsActive
        builder.Property(u => u.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasIndex(u => u.IsActive)
            .HasDatabaseName("ix_users_is_active");

        // LastLoginAt
        builder.Property(u => u.LastLoginAt)
            .HasColumnName("last_login_at")
            .HasColumnType("timestamp with time zone");

        // RefreshToken
        builder.Property(u => u.RefreshToken)
            .HasColumnName("refresh_token")
            .HasMaxLength(512);

        builder.HasIndex(u => u.RefreshToken)
            .HasDatabaseName("ix_users_refresh_token")
            .HasFilter("refresh_token IS NOT NULL");

        // RefreshTokenExpiryTime
        builder.Property(u => u.RefreshTokenExpiryTime)
            .HasColumnName("refresh_token_expiry_time")
            .HasColumnType("timestamp with time zone");

        // MasterId - Foreign key to Master user
        builder.Property(u => u.MasterId)
            .HasColumnName("master_id")
            .HasColumnType("uuid");

        builder.HasIndex(u => u.MasterId)
            .HasDatabaseName("ix_users_master_id")
            .HasFilter("master_id IS NOT NULL");

        // InvitationCode for Masters
        builder.Property(u => u.InvitationCode)
            .HasColumnName("invitation_code")
            .HasMaxLength(10);

        builder.HasIndex(u => u.InvitationCode)
            .IsUnique()
            .HasDatabaseName("ix_users_invitation_code")
            .HasFilter("invitation_code IS NOT NULL");

        // Self-referencing relationship: Player -> Master
        builder.HasOne(u => u.Master)
            .WithMany(u => u.Players)
            .HasForeignKey(u => u.MasterId)
            .OnDelete(DeleteBehavior.SetNull);

        // Audit fields
        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(u => u.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(256);

        builder.Property(u => u.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(u => u.UpdatedBy)
            .HasColumnName("updated_by")
            .HasMaxLength(256);

        // Ignore domain events (not persisted)
        builder.Ignore(u => u.DomainEvents);
    }
}
