using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loremaster.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for Document entity.
/// Configures table mapping, relationships, and indexes including pgvector support.
/// Uses snake_case naming convention for PostgreSQL compatibility.
/// </summary>
public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("documents");

        // Primary key
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        // Core properties with snake_case column names
        builder.Property(d => d.Title)
            .IsRequired()
            .HasMaxLength(500)
            .HasColumnName("title");

        builder.Property(d => d.Content)
            .IsRequired()
            .HasColumnName("content");

        builder.Property(d => d.Source)
            .HasMaxLength(1000)
            .HasColumnName("source");

        builder.Property(d => d.Metadata)
            .HasColumnType("jsonb")
            .HasColumnName("metadata");

        // pgvector column - Pgvector.Vector type maps automatically with UseVector()
        builder.Property(d => d.Embedding)
            .HasColumnType("vector(768)")
            .HasColumnName("embedding");

        builder.Property(d => d.EmbeddingDimensions)
            .HasColumnName("embedding_dimensions");
        
        // Owner relationship
        builder.Property(d => d.OwnerId)
            .HasColumnName("owner_id");
        
        // Game system association
        builder.Property(d => d.GameSystemId)
            .HasColumnName("game_system_id");

        // RAG source type enum
        builder.Property(d => d.SourceType)
            .HasConversion(
                v => v.HasValue ? v.Value.ToString() : null,
                v => !string.IsNullOrEmpty(v) ? Enum.Parse<RagSourceType>(v) : null)
            .HasMaxLength(50)
            .HasColumnName("source_type");
        
        // Chunk tracking
        builder.Property(d => d.ChunkIndex)
            .HasColumnName("chunk_index");
        
        builder.Property(d => d.ParentDocumentId)
            .HasColumnName("parent_document_id");

        // Audit columns (from AuditableEntity base class)
        builder.Property(d => d.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        
        builder.Property(d => d.UpdatedAt)
            .HasColumnName("updated_at");
        
        builder.Property(d => d.CreatedBy)
            .HasColumnName("created_by");
        
        builder.Property(d => d.UpdatedBy)
            .HasColumnName("updated_by");

        // Relationships
        builder.HasOne(d => d.Owner)
            .WithMany()
            .HasForeignKey(d => d.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.GameSystem)
            .WithMany()
            .HasForeignKey(d => d.GameSystemId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes with snake_case naming convention
        builder.HasIndex(d => d.OwnerId)
            .HasDatabaseName("ix_documents_owner_id");
        
        builder.HasIndex(d => d.GameSystemId)
            .HasDatabaseName("ix_documents_game_system_id");
        
        builder.HasIndex(d => d.ParentDocumentId)
            .HasDatabaseName("ix_documents_parent_document_id");
        
        builder.HasIndex(d => d.CreatedAt)
            .HasDatabaseName("ix_documents_created_at");
        
        builder.HasIndex(d => d.SourceType)
            .HasDatabaseName("ix_documents_source_type");

        // Note: pgvector index (ivfflat or hnsw) should be created in migration:
        // CREATE INDEX ON documents USING ivfflat (embedding vector_cosine_ops) WITH (lists = 100);
    }
}
