using LifeOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Persistence.Configurations;

public class StreakConfiguration : IEntityTypeConfiguration<Streak>
{
    public void Configure(EntityTypeBuilder<Streak> builder)
    {
        builder.ToTable("streaks");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.MetricCode)
            .HasMaxLength(50);

        builder.Property(e => e.CurrentStreakLength)
            .HasDefaultValue(0);

        builder.Property(e => e.LongestStreakLength)
            .HasDefaultValue(0);

        builder.Property(e => e.MissCount)
            .HasDefaultValue(0);

        builder.Property(e => e.MaxAllowedMisses)
            .HasDefaultValue(0);

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.UpdatedAt);

        builder.HasOne(e => e.User)
            .WithMany(u => u.Streaks)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Task)
            .WithMany(t => t.Streaks)
            .HasForeignKey(e => e.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.MetricDefinition)
            .WithMany(m => m.Streaks)
            .HasForeignKey(e => e.MetricCode)
            .HasPrincipalKey(m => m.Code)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.TaskId)
            .HasFilter("\"TaskId\" IS NOT NULL");
        builder.HasIndex(e => e.MetricCode)
            .HasFilter("\"MetricCode\" IS NOT NULL");
        builder.HasIndex(e => e.UserId)
            .HasFilter("\"IsActive\" = TRUE")
            .HasDatabaseName("idx_streaks_active");

        // Check constraint: either task_id or metric_code, not both
        builder.ToTable(t => t.HasCheckConstraint("chk_streak_link", 
            "(\"TaskId\" IS NOT NULL AND \"MetricCode\" IS NULL) OR (\"TaskId\" IS NULL AND \"MetricCode\" IS NOT NULL)"));
    }
}
