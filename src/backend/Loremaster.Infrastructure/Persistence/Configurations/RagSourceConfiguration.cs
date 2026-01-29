using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loremaster.Infrastructure.Persistence.Configurations;

public class RagSourceConfiguration : IEntityTypeConfiguration<RagSource>
{
    public void Configure(EntityTypeBuilder<RagSource> builder)
    {
        builder.ToTable("rag_source");

        builder.HasKey(rs => rs.Id);

        builder.Property(rs => rs.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(rs => rs.GameSystemId)
            .HasColumnName("game_system_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.HasOne(rs => rs.GameSystem)
            .WithMany(gs => gs.RagSources)
            .HasForeignKey(rs => rs.GameSystemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(rs => rs.GameSystemId)
            .HasDatabaseName("ix_rag_source_game_system_id");

        builder.Property(rs => rs.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(rs => rs.SourceType)
            .HasColumnName("source_type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(rs => rs.SourceType)
            .HasDatabaseName("ix_rag_source_source_type");

        builder.Property(rs => rs.Version)
            .HasColumnName("version")
            .HasMaxLength(50);

        builder.Property(rs => rs.ContentHash)
            .HasColumnName("content_hash")
            .HasMaxLength(64);

        builder.HasIndex(rs => rs.ContentHash)
            .HasDatabaseName("ix_rag_source_content_hash")
            .HasFilter("content_hash IS NOT NULL");

        builder.Property(rs => rs.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasIndex(rs => rs.IsActive)
            .HasDatabaseName("ix_rag_source_is_active")
            .HasFilter("is_active = true");

        builder.Property(rs => rs.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(rs => rs.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Ignore(rs => rs.CreatedBy);
        builder.Ignore(rs => rs.UpdatedBy);
        builder.Ignore(rs => rs.DomainEvents);
    }
}
