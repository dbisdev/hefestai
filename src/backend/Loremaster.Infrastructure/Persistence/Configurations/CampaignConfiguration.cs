using Loremaster.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loremaster.Infrastructure.Persistence.Configurations;

public class CampaignConfiguration : IEntityTypeConfiguration<Campaign>
{
    public void Configure(EntityTypeBuilder<Campaign> builder)
    {
        builder.ToTable("campaign");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(c => c.OwnerId)
            .HasColumnName("owner_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.HasOne(c => c.Owner)
            .WithMany(u => u.OwnedCampaigns)
            .HasForeignKey(c => c.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.OwnerId)
            .HasDatabaseName("ix_campaign_owner_id")
            .HasFilter("deleted_at IS NULL");

        builder.Property(c => c.GameSystemId)
            .HasColumnName("game_system_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.HasOne(c => c.GameSystem)
            .WithMany(gs => gs.Campaigns)
            .HasForeignKey(c => c.GameSystemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.GameSystemId)
            .HasDatabaseName("ix_campaign_game_system_id")
            .HasFilter("deleted_at IS NULL");

        builder.Property(c => c.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.Property(c => c.JoinCode)
            .HasColumnName("join_code")
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(c => c.JoinCode)
            .IsUnique()
            .HasDatabaseName("uq_campaign_join_code");

        builder.HasIndex(c => c.JoinCode)
            .HasDatabaseName("ix_campaign_join_code")
            .HasFilter("deleted_at IS NULL");

        builder.Property(c => c.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasIndex(c => c.IsActive)
            .HasDatabaseName("ix_campaign_is_active")
            .HasFilter("deleted_at IS NULL AND is_active = true");

        builder.Property(c => c.Settings)
            .HasColumnName("settings")
            .HasColumnType("jsonb");

        builder.HasIndex(c => c.Settings)
            .HasDatabaseName("ix_campaign_settings")
            .HasMethod("gin")
            .HasFilter("settings IS NOT NULL");

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(c => c.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(c => c.DeletedAt)
            .HasDatabaseName("ix_campaign_deleted_at")
            .HasFilter("deleted_at IS NOT NULL");

        builder.Ignore(c => c.CreatedBy);
        builder.Ignore(c => c.UpdatedBy);
        builder.Ignore(c => c.DomainEvents);
    }
}
