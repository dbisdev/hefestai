using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loremaster.Infrastructure.Persistence.Configurations;

public class LoreEntityConfiguration : IEntityTypeConfiguration<LoreEntity>
{
    public void Configure(EntityTypeBuilder<LoreEntity> builder)
    {
        builder.ToTable("lore_entity");

        builder.HasKey(le => le.Id);

        builder.Property(le => le.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(le => le.CampaignId)
            .HasColumnName("campaign_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.HasOne(le => le.Campaign)
            .WithMany(c => c.LoreEntities)
            .HasForeignKey(le => le.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(le => le.OwnerId)
            .HasColumnName("owner_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.HasOne(le => le.Owner)
            .WithMany(u => u.OwnedLoreEntities)
            .HasForeignKey(le => le.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(le => le.GenerationRequestId)
            .HasColumnName("generation_request_id")
            .HasColumnType("uuid");

        builder.HasOne(le => le.GenerationRequest)
            .WithMany(gr => gr.GeneratedEntities)
            .HasForeignKey(le => le.GenerationRequestId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(le => le.EntityType)
            .HasColumnName("entity_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(le => le.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(le => le.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.Property(le => le.OwnershipType)
            .HasColumnName("ownership_type")
            .HasColumnType("ownership_type")
            .HasDefaultValue(OwnershipType.Master)
            .IsRequired();

        builder.Property(le => le.Visibility)
            .HasColumnName("visibility")
            .HasColumnType("visibility_level")
            .HasDefaultValue(VisibilityLevel.Campaign)
            .IsRequired();

        builder.Property(le => le.IsTemplate)
            .HasColumnName("is_template")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(le => le.ImageUrl)
            .HasColumnName("image_url")
            .HasMaxLength(500);

        builder.Property(le => le.Attributes)
            .HasColumnName("attributes")
            .HasColumnType("jsonb");

        builder.Property(le => le.Metadata)
            .HasColumnName("metadata")
            .HasColumnType("jsonb");

        builder.Property(le => le.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(le => le.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(le => le.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamp with time zone");

        // =====================================================================
        // INDEXES - Optimized for common query patterns
        // =====================================================================

        // PRIMARY QUERY: List entities in campaign by type
        builder.HasIndex(le => new { le.CampaignId, le.EntityType })
            .HasDatabaseName("ix_lore_entity_campaign_type")
            .HasFilter("deleted_at IS NULL");

        // ACCESS CONTROL: Filter by visibility within campaign
        builder.HasIndex(le => new { le.CampaignId, le.Visibility })
            .HasDatabaseName("ix_lore_entity_campaign_visibility")
            .HasFilter("deleted_at IS NULL");

        // MY ENTITIES: Owner's entities across campaigns
        builder.HasIndex(le => le.OwnerId)
            .HasDatabaseName("ix_lore_entity_owner_id")
            .HasFilter("deleted_at IS NULL");

        // PLAYER CHARACTERS: Quick lookup for player-owned characters
        builder.HasIndex(le => new { le.CampaignId, le.OwnerId })
            .HasDatabaseName("ix_lore_entity_player_characters")
            .HasFilter("entity_type = 'character' AND ownership_type = 'Player' AND deleted_at IS NULL");

        // TEMPLATES: Find reusable templates
        builder.HasIndex(le => new { le.CampaignId, le.EntityType })
            .HasDatabaseName("ix_lore_entity_templates")
            .HasFilter("is_template = true AND deleted_at IS NULL");

        // GENERATION TRACKING: Entities from AI generation
        builder.HasIndex(le => le.GenerationRequestId)
            .HasDatabaseName("ix_lore_entity_generation")
            .HasFilter("generation_request_id IS NOT NULL AND deleted_at IS NULL");

        // SOFT DELETE: Filter deleted entities
        builder.HasIndex(le => le.DeletedAt)
            .HasDatabaseName("ix_lore_entity_deleted_at")
            .HasFilter("deleted_at IS NOT NULL");

        // Note: GIN indexes for trigram (name search), full-text search, and JSONB 
        // are created in init.sql as they require extensions not available in EF Core

        builder.Ignore(le => le.CreatedBy);
        builder.Ignore(le => le.UpdatedBy);
        builder.Ignore(le => le.DomainEvents);
    }
}
