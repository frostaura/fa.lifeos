using LifeOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Persistence.Configurations;

public class AdherenceSnapshotConfiguration : IEntityTypeConfiguration<AdherenceSnapshot>
{
    public void Configure(EntityTypeBuilder<AdherenceSnapshot> builder)
    {
        builder.ToTable("adherence_snapshots");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id");

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.Timestamp)
            .HasColumnName("timestamp");

        builder.Property(e => e.Score)
            .HasColumnName("score")
            .HasPrecision(5, 2);

        builder.Property(e => e.TimeWindowDays)
            .HasColumnName("time_window_days");

        builder.Property(e => e.TasksConsidered)
            .HasColumnName("tasks_considered");

        builder.Property(e => e.TasksCompleted)
            .HasColumnName("tasks_completed");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasOne(e => e.User)
            .WithMany(u => u.AdherenceSnapshots)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.UserId, e.Timestamp });
    }
}
