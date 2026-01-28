using Loremaster.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loremaster.Infrastructure.Persistence.Configurations;

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

        // Relationships
        builder.HasOne(d => d.Owner)
            .WithMany()
            .HasForeignKey(d => d.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.Project)
            .WithMany()
            .HasForeignKey(d => d.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(d => d.OwnerId);
        builder.HasIndex(d => d.ProjectId);
        builder.HasIndex(d => d.CreatedAt);

        // Note: pgvector index (ivfflat or hnsw) should be created in migration:
        // CREATE INDEX ON "Documents" USING ivfflat ("Embedding" vector_cosine_ops) WITH (lists = 100);
    }
}
