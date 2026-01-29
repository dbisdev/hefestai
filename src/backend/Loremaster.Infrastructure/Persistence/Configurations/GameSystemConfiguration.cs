using Loremaster.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loremaster.Infrastructure.Persistence.Configurations;

public class GameSystemConfiguration : IEntityTypeConfiguration<GameSystem>
{
    public void Configure(EntityTypeBuilder<GameSystem> builder)
    {
        builder.ToTable("game_system");

        builder.HasKey(gs => gs.Id);

        builder.Property(gs => gs.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(gs => gs.Code)
            .HasColumnName("code")
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(gs => gs.Code)
            .IsUnique()
            .HasDatabaseName("uq_game_system_code");

        builder.Property(gs => gs.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(gs => gs.Publisher)
            .HasColumnName("publisher")
            .HasMaxLength(100);

        builder.Property(gs => gs.Version)
            .HasColumnName("version")
            .HasMaxLength(50);

        builder.Property(gs => gs.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.Property(gs => gs.SupportedEntityTypes)
            .HasColumnName("supported_entity_types")
            .HasColumnType("varchar(50)[]")
            .HasDefaultValueSql("'{}'");

        builder.Property(gs => gs.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasIndex(gs => gs.IsActive)
            .HasDatabaseName("ix_game_system_is_active")
            .HasFilter("is_active = true");

        builder.Property(gs => gs.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(gs => gs.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Ignore(gs => gs.CreatedBy);
        builder.Ignore(gs => gs.UpdatedBy);
        builder.Ignore(gs => gs.DomainEvents);
    }
}
