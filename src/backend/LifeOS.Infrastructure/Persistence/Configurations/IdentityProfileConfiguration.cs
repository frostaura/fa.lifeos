using LifeOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Persistence.Configurations;

public class IdentityProfileConfiguration : IEntityTypeConfiguration<IdentityProfile>
{
    public void Configure(EntityTypeBuilder<IdentityProfile> builder)
    {
        builder.ToTable("identity_profiles");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id");

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.Archetype)
            .HasColumnName("archetype")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.ArchetypeDescription)
            .HasColumnName("archetype_description");

        builder.Property(e => e.Values)
            .HasColumnName("values")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(e => e.PrimaryStatTargets)
            .HasColumnName("primary_stat_targets")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(e => e.LinkedMilestoneIds)
            .HasColumnName("linked_milestones")
            .HasColumnType("jsonb");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        // One user has one identity profile
        builder.HasIndex(e => e.UserId)
            .IsUnique();

        builder.HasOne(e => e.User)
            .WithOne(u => u.IdentityProfile)
            .HasForeignKey<IdentityProfile>(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
