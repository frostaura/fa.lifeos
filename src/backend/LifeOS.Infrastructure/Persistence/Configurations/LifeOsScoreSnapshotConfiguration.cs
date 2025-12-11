using LifeOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Persistence.Configurations;

public class LifeOsScoreSnapshotConfiguration : IEntityTypeConfiguration<LifeOsScoreSnapshot>
{
    public void Configure(EntityTypeBuilder<LifeOsScoreSnapshot> builder)
    {
        builder.ToTable("lifeos_score_snapshots");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id");

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.Timestamp)
            .HasColumnName("timestamp");

        builder.Property(e => e.LifeScore)
            .HasColumnName("life_score")
            .HasPrecision(5, 2);

        builder.Property(e => e.HealthIndex)
            .HasColumnName("health_index")
            .HasPrecision(5, 2);

        builder.Property(e => e.AdherenceIndex)
            .HasColumnName("adherence_index")
            .HasPrecision(5, 2);

        builder.Property(e => e.WealthHealthScore)
            .HasColumnName("wealth_health_score")
            .HasPrecision(5, 2);

        builder.Property(e => e.LongevityYearsAdded)
            .HasColumnName("longevity_years_added")
            .HasPrecision(6, 2);

        builder.Property(e => e.DimensionScores)
            .HasColumnName("dimension_scores")
            .HasColumnType("jsonb");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasOne(e => e.User)
            .WithMany(u => u.LifeOsScoreSnapshots)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.UserId, e.Timestamp });
    }
}
