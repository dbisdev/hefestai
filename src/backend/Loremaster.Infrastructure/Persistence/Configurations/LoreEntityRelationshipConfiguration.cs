using Loremaster.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loremaster.Infrastructure.Persistence.Configurations;

public class LoreEntityRelationshipConfiguration : IEntityTypeConfiguration<LoreEntityRelationship>
{
    public void Configure(EntityTypeBuilder<LoreEntityRelationship> builder)
    {
        builder.ToTable("lore_entity_relationship");

        builder.HasKey(ler => ler.Id);

        builder.Property(ler => ler.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(ler => ler.SourceEntityId)
            .HasColumnName("source_entity_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.HasOne(ler => ler.SourceEntity)
            .WithMany(le => le.OutgoingRelationships)
            .HasForeignKey(ler => ler.SourceEntityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ler => ler.SourceEntityId)
            .HasDatabaseName("ix_lore_entity_relationship_source");

        builder.Property(ler => ler.TargetEntityId)
            .HasColumnName("target_entity_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.HasOne(ler => ler.TargetEntity)
            .WithMany(le => le.IncomingRelationships)
            .HasForeignKey(ler => ler.TargetEntityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ler => ler.TargetEntityId)
            .HasDatabaseName("ix_lore_entity_relationship_target");

        builder.Property(ler => ler.RelationshipType)
            .HasColumnName("relationship_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(ler => ler.RelationshipType)
            .HasDatabaseName("ix_lore_entity_relationship_type");

        builder.HasIndex(ler => new { ler.SourceEntityId, ler.TargetEntityId, ler.RelationshipType })
            .IsUnique()
            .HasDatabaseName("uq_lore_entity_relationship");

        builder.Property(ler => ler.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.Property(ler => ler.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.HasCheckConstraint(
            "chk_no_self_relationship",
            "source_entity_id != target_entity_id");

        builder.Ignore(ler => ler.UpdatedAt);
        builder.Ignore(ler => ler.DomainEvents);
    }
}
