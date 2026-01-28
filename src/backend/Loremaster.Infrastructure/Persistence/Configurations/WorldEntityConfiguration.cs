using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loremaster.Infrastructure.Persistence.Configurations;

public class WorldEntityConfiguration : IEntityTypeConfiguration<WorldEntity>
{
    public void Configure(EntityTypeBuilder<WorldEntity> builder)
    {
        // Table
        builder.ToTable("world_entities");

        // Primary Key (UUID)
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        // Name
        builder.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        // Type
        builder.Property(e => e.Type)
            .HasColumnName("type")
            .HasMaxLength(100)
            .IsRequired();

        // Meta
        builder.Property(e => e.Meta)
            .HasColumnName("meta")
            .HasMaxLength(200);

        // Image (can be base64 or URL, so allow large size)
        builder.Property(e => e.Image)
            .HasColumnName("image")
            .HasColumnType("text");

        // Category
        builder.Property(e => e.Category)
            .HasColumnName("category")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(e => e.Category)
            .HasDatabaseName("ix_world_entities_category");

        // Description
        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        // StatsJson (stored as JSONB in PostgreSQL)
        builder.Property(e => e.StatsJson)
            .HasColumnName("stats")
            .HasColumnType("jsonb");

        // CreatorId - Foreign key to User (Master)
        builder.Property(e => e.CreatorId)
            .HasColumnName("creator_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.HasIndex(e => e.CreatorId)
            .HasDatabaseName("ix_world_entities_creator_id");

        // Relationship: WorldEntity -> User (Creator/Master)
        builder.HasOne(e => e.Creator)
            .WithMany()
            .HasForeignKey(e => e.CreatorId)
            .OnDelete(DeleteBehavior.Cascade);

        // Composite index for efficient queries
        builder.HasIndex(e => new { e.CreatorId, e.Category })
            .HasDatabaseName("ix_world_entities_creator_category");

        // Audit fields
        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(e => e.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(256);

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.UpdatedBy)
            .HasColumnName("updated_by")
            .HasMaxLength(256);

        // Ignore domain events (not persisted)
        builder.Ignore(e => e.DomainEvents);
    }
}
