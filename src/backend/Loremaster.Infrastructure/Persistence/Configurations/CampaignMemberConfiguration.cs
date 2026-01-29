using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loremaster.Infrastructure.Persistence.Configurations;

public class CampaignMemberConfiguration : IEntityTypeConfiguration<CampaignMember>
{
    public void Configure(EntityTypeBuilder<CampaignMember> builder)
    {
        builder.ToTable("campaign_member");

        builder.HasKey(cm => cm.Id);

        builder.Property(cm => cm.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(cm => cm.CampaignId)
            .HasColumnName("campaign_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.HasOne(cm => cm.Campaign)
            .WithMany(c => c.Members)
            .HasForeignKey(cm => cm.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(cm => cm.CampaignId)
            .HasDatabaseName("ix_campaign_member_campaign_id");

        builder.Property(cm => cm.UserId)
            .HasColumnName("user_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.HasOne(cm => cm.User)
            .WithMany(u => u.CampaignMemberships)
            .HasForeignKey(cm => cm.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(cm => cm.UserId)
            .HasDatabaseName("ix_campaign_member_user_id");

        builder.HasIndex(cm => new { cm.CampaignId, cm.UserId })
            .IsUnique()
            .HasDatabaseName("uq_campaign_member");

        builder.Property(cm => cm.Role)
            .HasColumnName("role")
            .HasColumnType("campaign_role")
            .HasDefaultValue(CampaignRole.Player)
            .IsRequired();

        builder.HasIndex(cm => cm.Role)
            .HasDatabaseName("ix_campaign_member_role");

        builder.Property(cm => cm.JoinedAt)
            .HasColumnName("joined_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(cm => cm.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(cm => cm.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Ignore(cm => cm.CreatedBy);
        builder.Ignore(cm => cm.UpdatedBy);
        builder.Ignore(cm => cm.DomainEvents);
    }
}
