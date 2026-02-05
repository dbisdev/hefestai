using Loremaster.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loremaster.Infrastructure.Persistence.Configurations;

public class GenerationResultConfiguration : IEntityTypeConfiguration<GenerationResult>
{
    public void Configure(EntityTypeBuilder<GenerationResult> builder)
    {
        builder.ToTable("generation_result");

        builder.HasKey(gr => gr.Id);

        builder.Property(gr => gr.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(gr => gr.GenerationRequestId)
            .HasColumnName("generation_request_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.HasOne(gr => gr.GenerationRequest)
            .WithMany(req => req.Results)
            .HasForeignKey(gr => gr.GenerationRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(gr => gr.ResultType)
            .HasColumnName("result_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(gr => gr.SequenceOrder)
            .HasColumnName("sequence_order")
            .HasDefaultValue(1)
            .IsRequired();

        // Changed from TEXT to JSONB
        builder.Property(gr => gr.RawOutput)
            .HasColumnName("raw_output")
            .HasColumnType("jsonb");

        builder.Property(gr => gr.StructuredOutput)
            .HasColumnName("structured_output")
            .HasColumnType("jsonb");

        builder.Property(gr => gr.ModelName)
            .HasColumnName("model_name")
            .HasMaxLength(100);

        builder.Property(gr => gr.ModelParameters)
            .HasColumnName("model_parameters")
            .HasColumnType("jsonb");

        builder.Property(gr => gr.TokenUsage)
            .HasColumnName("token_usage")
            .HasColumnType("jsonb");

        builder.Property(gr => gr.ConfidenceScore)
            .HasColumnName("confidence_score")
            .HasColumnType("decimal(5,4)");

        builder.ToTable(t => t.HasCheckConstraint(
            "chk_confidence_score",
            "confidence_score IS NULL OR (confidence_score >= 0 AND confidence_score <= 1)"));

        builder.Property(gr => gr.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        // =====================================================================
        // INDEXES
        // =====================================================================

        // Results for a request
        builder.HasIndex(gr => gr.GenerationRequestId)
            .HasDatabaseName("ix_generation_result_request");

        // Filter by result type
        builder.HasIndex(gr => gr.ResultType)
            .HasDatabaseName("ix_generation_result_type");

        // Analytics: by model
        builder.HasIndex(gr => gr.ModelName)
            .HasDatabaseName("ix_generation_result_model")
            .HasFilter("model_name IS NOT NULL");

        // Note: GIN indexes for JSONB (raw_output, structured_output) 
        // are created in init.sql

        builder.Ignore(gr => gr.UpdatedAt);
        builder.Ignore(gr => gr.DomainEvents);
    }
}
