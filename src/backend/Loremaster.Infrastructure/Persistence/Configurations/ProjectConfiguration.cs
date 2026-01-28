using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loremaster.Infrastructure.Persistence.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(p => p.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasColumnName("description")
            .HasMaxLength(2000);

        builder.Property(p => p.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(ProjectStatus.Active);

        builder.Property(p => p.OwnerId)
            .HasColumnName("owner_id")
            .IsRequired();

        builder.Property(p => p.ArchivedAt)
            .HasColumnName("archived_at");

        // Audit fields
        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(p => p.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(256);

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(p => p.UpdatedBy)
            .HasColumnName("updated_by")
            .HasMaxLength(256);

        // Relationships
        builder.HasOne(p => p.Owner)
            .WithMany()
            .HasForeignKey(p => p.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(p => p.OwnerId)
            .HasDatabaseName("ix_projects_owner_id");

        builder.HasIndex(p => p.Status)
            .HasDatabaseName("ix_projects_status");

        builder.HasIndex(p => new { p.OwnerId, p.Status })
            .HasDatabaseName("ix_projects_owner_status");
    }
}
