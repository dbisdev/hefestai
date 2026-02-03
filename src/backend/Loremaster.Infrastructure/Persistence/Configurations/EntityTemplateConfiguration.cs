using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loremaster.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for EntityTemplate entity.
/// Defines table schema, relationships, and indexes.
/// </summary>
public class EntityTemplateConfiguration : IEntityTypeConfiguration<EntityTemplate>
{
    public void Configure(EntityTypeBuilder<EntityTemplate> builder)
    {
        builder.ToTable("entity_template");

        builder.HasKey(et => et.Id);

        builder.Property(et => et.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(et => et.EntityTypeName)
            .HasColumnName("entity_type_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(et => et.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(et => et.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.Property(et => et.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<TemplateStatus>(v))
            .IsRequired();

        builder.Property(et => et.FieldDefinitionsJson)
            .HasColumnName("field_definitions")
            .HasColumnType("jsonb")
            .HasDefaultValue("[]")
            .IsRequired();

        builder.Property(et => et.IconHint)
            .HasColumnName("icon_hint")
            .HasMaxLength(50);

        builder.Property(et => et.Version)
            .HasColumnName("version")
            .HasMaxLength(50);

        builder.Property(et => et.ReviewNotes)
            .HasColumnName("review_notes")
            .HasColumnType("text");

        builder.Property(et => et.ConfirmedAt)
            .HasColumnName("confirmed_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(et => et.ConfirmedByUserId)
            .HasColumnName("confirmed_by_user_id")
            .HasColumnType("uuid");

        // Relationships
        builder.Property(et => et.GameSystemId)
            .HasColumnName("game_system_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.HasOne(et => et.GameSystem)
            .WithMany()
            .HasForeignKey(et => et.GameSystemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(et => et.SourceDocumentId)
            .HasColumnName("source_document_id")
            .HasColumnType("uuid");

        builder.HasOne(et => et.SourceDocument)
            .WithMany()
            .HasForeignKey(et => et.SourceDocumentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(et => et.OwnerId)
            .HasColumnName("owner_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.HasOne(et => et.Owner)
            .WithMany()
            .HasForeignKey(et => et.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Audit fields
        builder.Property(et => et.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(et => et.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        builder.Ignore(et => et.CreatedBy);
        builder.Ignore(et => et.UpdatedBy);
        builder.Ignore(et => et.DomainEvents);

        // Indexes
        builder.HasIndex(et => et.GameSystemId)
            .HasDatabaseName("ix_entity_template_game_system_id");

        builder.HasIndex(et => et.OwnerId)
            .HasDatabaseName("ix_entity_template_owner_id");

        builder.HasIndex(et => et.Status)
            .HasDatabaseName("ix_entity_template_status");

        // Unique constraint: entity type name + game system + owner
        builder.HasIndex(et => new { et.GameSystemId, et.OwnerId, et.EntityTypeName })
            .IsUnique()
            .HasDatabaseName("uq_entity_template_game_system_owner_type");

        // Filtered index for confirmed templates (for fast lookups)
        builder.HasIndex(et => new { et.GameSystemId, et.OwnerId, et.EntityTypeName })
            .HasDatabaseName("ix_entity_template_confirmed")
            .HasFilter("status = 'Confirmed'");
    }
}
