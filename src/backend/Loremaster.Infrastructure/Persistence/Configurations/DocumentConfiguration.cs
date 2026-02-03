using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loremaster.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for Document entity.
/// Configures table mapping, relationships, and indexes including pgvector support.
/// </summary>
public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Documents");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(d => d.Content)
            .IsRequired();

        builder.Property(d => d.Source)
            .HasMaxLength(1000);

        builder.Property(d => d.Metadata)
            .HasColumnType("jsonb");

        // pgvector column - Pgvector.Vector type maps automatically with UseVector()
        builder.Property(d => d.Embedding)
            .HasColumnType("vector(768)");

        builder.Property(d => d.EmbeddingDimensions);
        
        // RAG source type enum
        builder.Property(d => d.SourceType)
            .HasConversion(
                v => v.HasValue ? v.Value.ToString() : null,
                v => !string.IsNullOrEmpty(v) ? Enum.Parse<RagSourceType>(v) : null)
            .HasMaxLength(50);
        
        // Chunk tracking
        builder.Property(d => d.ChunkIndex);
        builder.Property(d => d.ParentDocumentId);

        // Relationships
        builder.HasOne(d => d.Owner)
            .WithMany()
            .HasForeignKey(d => d.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.Project)
            .WithMany()
            .HasForeignKey(d => d.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);
        
        builder.HasOne(d => d.GameSystem)
            .WithMany()
            .HasForeignKey(d => d.GameSystemId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(d => d.OwnerId);
        builder.HasIndex(d => d.ProjectId);
        builder.HasIndex(d => d.GameSystemId);
        builder.HasIndex(d => d.ParentDocumentId);
        builder.HasIndex(d => d.CreatedAt);
        builder.HasIndex(d => d.SourceType);

        // Note: pgvector index (ivfflat or hnsw) should be created in migration:
        // CREATE INDEX ON "Documents" USING ivfflat ("Embedding" vector_cosine_ops) WITH (lists = 100);
    }
}
