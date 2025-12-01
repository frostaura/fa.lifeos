using LifeOS.Domain.Entities;
using LifeOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Persistence.Configurations;

public class LifeTaskConfiguration : IEntityTypeConfiguration<LifeTask>
{
    public void Configure(EntityTypeBuilder<LifeTask> builder)
    {
        builder.ToTable("tasks");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.Title)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.Description);

        builder.Property(e => e.TaskType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.Frequency)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.LinkedMetricCode)
            .HasMaxLength(50);

        builder.Property(e => e.IsCompleted)
            .HasDefaultValue(false);

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);

        builder.Property(e => e.Tags);

        builder.Property(e => e.Metadata)
            .HasColumnType("jsonb");

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.UpdatedAt);

        builder.HasOne(e => e.User)
            .WithMany(u => u.Tasks)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Dimension)
            .WithMany(d => d.Tasks)
            .HasForeignKey(e => e.DimensionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Milestone)
            .WithMany(m => m.Tasks)
            .HasForeignKey(e => e.MilestoneId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.DimensionId);
        builder.HasIndex(e => e.MilestoneId);
        builder.HasIndex(e => new { e.UserId, e.TaskType });
        builder.HasIndex(e => new { e.UserId, e.ScheduledDate })
            .HasFilter("\"ScheduledDate\" IS NOT NULL");
        builder.HasIndex(e => e.Tags)
            .HasMethod("gin");
    }
}
