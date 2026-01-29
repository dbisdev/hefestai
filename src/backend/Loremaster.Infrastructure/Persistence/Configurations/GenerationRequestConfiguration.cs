using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loremaster.Infrastructure.Persistence.Configurations;

public class GenerationRequestConfiguration : IEntityTypeConfiguration<GenerationRequest>
{
    public void Configure(EntityTypeBuilder<GenerationRequest> builder)
    {
        builder.ToTable("generation_request");

        builder.HasKey(gr => gr.Id);

        builder.Property(gr => gr.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(gr => gr.UserId)
            .HasColumnName("user_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.HasOne(gr => gr.User)
            .WithMany(u => u.GenerationRequests)
            .HasForeignKey(gr => gr.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(gr => gr.UserId)
            .HasDatabaseName("ix_generation_request_user_id");

        builder.Property(gr => gr.CampaignId)
            .HasColumnName("campaign_id")
            .HasColumnType("uuid");

        builder.HasOne(gr => gr.Campaign)
            .WithMany(c => c.GenerationRequests)
            .HasForeignKey(gr => gr.CampaignId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(gr => gr.CampaignId)
            .HasDatabaseName("ix_generation_request_campaign_id")
            .HasFilter("campaign_id IS NOT NULL");

        builder.Property(gr => gr.RequestType)
            .HasColumnName("request_type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(gr => gr.RequestType)
            .HasDatabaseName("ix_generation_request_request_type");

        builder.Property(gr => gr.TargetEntityType)
            .HasColumnName("target_entity_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(gr => gr.TargetEntityType)
            .HasDatabaseName("ix_generation_request_target_entity_type");

        builder.Property(gr => gr.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(GenerationStatus.Pending)
            .IsRequired();

        builder.HasIndex(gr => gr.Status)
            .HasDatabaseName("ix_generation_request_status");

        builder.Property(gr => gr.InputPrompt)
            .HasColumnName("input_prompt")
            .HasColumnType("text");

        builder.Property(gr => gr.InputParameters)
            .HasColumnName("input_parameters")
            .HasColumnType("jsonb");

        builder.HasIndex(gr => gr.InputParameters)
            .HasDatabaseName("ix_generation_request_input_parameters")
            .HasMethod("gin")
            .HasFilter("input_parameters IS NOT NULL");

        builder.Property(gr => gr.ErrorMessage)
            .HasColumnName("error_message")
            .HasColumnType("text");

        builder.Property(gr => gr.ProcessingStartedAt)
            .HasColumnName("processing_started_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(gr => gr.ProcessingCompletedAt)
            .HasColumnName("processing_completed_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(gr => gr.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.HasIndex(gr => gr.CreatedAt)
            .HasDatabaseName("ix_generation_request_created_at")
            .IsDescending();

        builder.Property(gr => gr.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Ignore(gr => gr.CreatedBy);
        builder.Ignore(gr => gr.UpdatedBy);
        builder.Ignore(gr => gr.DomainEvents);
    }
}
