using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loremaster.Infrastructure.Persistence.Configurations;

public class LoreEntityImportConfiguration : IEntityTypeConfiguration<LoreEntityImport>
{
    public void Configure(EntityTypeBuilder<LoreEntityImport> builder)
    {
        builder.ToTable("lore_entity_import");

        builder.HasKey(lei => lei.Id);

        builder.Property(lei => lei.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(lei => lei.LoreEntityId)
            .HasColumnName("lore_entity_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.HasOne(lei => lei.LoreEntity)
            .WithMany(le => le.Imports)
            .HasForeignKey(lei => lei.LoreEntityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(lei => lei.LoreEntityId)
            .HasDatabaseName("ix_lore_entity_import_entity_id");

        builder.Property(lei => lei.UserId)
            .HasColumnName("user_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.HasOne(lei => lei.User)
            .WithMany(u => u.Imports)
            .HasForeignKey(lei => lei.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(lei => lei.UserId)
            .HasDatabaseName("ix_lore_entity_import_user_id");

        builder.Property(lei => lei.ImportType)
            .HasColumnName("import_type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(lei => lei.ImportType)
            .HasDatabaseName("ix_lore_entity_import_type");

        builder.Property(lei => lei.SourceFilename)
            .HasColumnName("source_filename")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(lei => lei.SourceFileUrl)
            .HasColumnName("source_file_url")
            .HasMaxLength(500);

        builder.Property(lei => lei.FileHash)
            .HasColumnName("file_hash")
            .HasMaxLength(64);

        builder.HasIndex(lei => lei.FileHash)
            .HasDatabaseName("ix_lore_entity_import_file_hash")
            .HasFilter("file_hash IS NOT NULL");

        builder.Property(lei => lei.ExtractionResult)
            .HasColumnName("extraction_result")
            .HasColumnType("jsonb");

        builder.HasIndex(lei => lei.ExtractionResult)
            .HasDatabaseName("ix_lore_entity_import_extraction_result")
            .HasMethod("gin")
            .HasFilter("extraction_result IS NOT NULL");

        builder.Property(lei => lei.FieldMapping)
            .HasColumnName("field_mapping")
            .HasColumnType("jsonb");

        builder.Property(lei => lei.ProcessingStatus)
            .HasColumnName("processing_status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(GenerationStatus.Pending)
            .IsRequired();

        builder.HasIndex(lei => lei.ProcessingStatus)
            .HasDatabaseName("ix_lore_entity_import_status");

        builder.Property(lei => lei.ErrorDetails)
            .HasColumnName("error_details")
            .HasColumnType("text");

        builder.Property(lei => lei.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(lei => lei.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Ignore(lei => lei.CreatedBy);
        builder.Ignore(lei => lei.UpdatedBy);
        builder.Ignore(lei => lei.DomainEvents);
    }
}
