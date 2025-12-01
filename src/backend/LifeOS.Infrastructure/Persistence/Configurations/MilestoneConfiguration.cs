using LifeOS.Domain.Entities;
using LifeOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Persistence.Configurations;

public class MilestoneConfiguration : IEntityTypeConfiguration<Milestone>
{
    public void Configure(EntityTypeBuilder<Milestone> builder)
    {
        builder.ToTable("milestones");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.Title)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.Description);

        builder.Property(e => e.TargetMetricCode)
            .HasMaxLength(50);

        builder.Property(e => e.TargetMetricValue)
            .HasPrecision(18, 4);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.UpdatedAt);

        builder.HasOne(e => e.User)
            .WithMany(u => u.Milestones)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Dimension)
            .WithMany(d => d.Milestones)
            .HasForeignKey(e => e.DimensionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.DimensionId);
        builder.HasIndex(e => new { e.UserId, e.Status })
            .HasFilter("\"Status\" = 'Active'");
    }
}
