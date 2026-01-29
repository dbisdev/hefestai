using Loremaster.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loremaster.Infrastructure.Persistence.Configurations;

public class GenerationResultSourceConfiguration : IEntityTypeConfiguration<GenerationResultSource>
{
    public void Configure(EntityTypeBuilder<GenerationResultSource> builder)
    {
        builder.ToTable("generation_result_source");

        // Composite primary key
        builder.HasKey(grs => new { grs.GenerationResultId, grs.RagSourceId });

        builder.Property(grs => grs.GenerationResultId)
            .HasColumnName("generation_result_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.HasOne(grs => grs.GenerationResult)
            .WithMany(gr => gr.Sources)
            .HasForeignKey(grs => grs.GenerationResultId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(grs => grs.RagSourceId)
            .HasColumnName("rag_source_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.HasOne(grs => grs.RagSource)
            .WithMany(rs => rs.UsedInResults)
            .HasForeignKey(grs => grs.RagSourceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(grs => grs.RagSourceId)
            .HasDatabaseName("ix_generation_result_source_rag");

        builder.Property(grs => grs.RelevanceScore)
            .HasColumnName("relevance_score")
            .HasColumnType("decimal(5,4)");

        builder.HasCheckConstraint(
            "chk_relevance_score",
            "relevance_score IS NULL OR (relevance_score >= 0 AND relevance_score <= 1)");

        builder.Property(grs => grs.Excerpt)
            .HasColumnName("excerpt")
            .HasColumnType("text");
    }
}
