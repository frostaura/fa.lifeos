using LifeOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Persistence.Configurations;

public class ReviewSnapshotConfiguration : IEntityTypeConfiguration<ReviewSnapshot>
{
    public void Configure(EntityTypeBuilder<ReviewSnapshot> builder)
    {
        builder.ToTable("review_snapshots");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id");

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.ReviewType)
            .HasColumnName("review_type")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.PeriodStart)
            .HasColumnName("period_start")
            .IsRequired();

        builder.Property(e => e.PeriodEnd)
            .HasColumnName("period_end")
            .IsRequired();

        builder.Property(e => e.HealthIndexCurrent)
            .HasColumnName("health_index_current")
            .HasPrecision(8, 4);

        builder.Property(e => e.AdherenceIndexCurrent)
            .HasColumnName("adherence_index_current")
            .HasPrecision(8, 4);

        builder.Property(e => e.WealthHealthCurrent)
            .HasColumnName("wealth_health_current")
            .HasPrecision(8, 4);

        builder.Property(e => e.LongevityCurrent)
            .HasColumnName("longevity_current")
            .HasPrecision(8, 4);

        builder.Property(e => e.HealthIndexDelta)
            .HasColumnName("health_index_delta")
            .HasPrecision(8, 4);

        builder.Property(e => e.AdherenceIndexDelta)
            .HasColumnName("adherence_index_delta")
            .HasPrecision(8, 4);

        builder.Property(e => e.WealthHealthDelta)
            .HasColumnName("wealth_health_delta")
            .HasPrecision(8, 4);

        builder.Property(e => e.LongevityDelta)
            .HasColumnName("longevity_delta")
            .HasPrecision(8, 4);

        builder.Property(e => e.TopStreaks)
            .HasColumnName("top_streaks")
            .HasColumnType("jsonb");

        builder.Property(e => e.RecommendedActions)
            .HasColumnName("recommended_actions")
            .HasColumnType("jsonb");

        builder.Property(e => e.PrimaryStatsCurrent)
            .HasColumnName("primary_stats_current")
            .HasColumnType("jsonb");

        builder.Property(e => e.PrimaryStatsDelta)
            .HasColumnName("primary_stats_delta")
            .HasColumnType("jsonb");

        builder.Property(e => e.ScenarioComparison)
            .HasColumnName("scenario_comparison")
            .HasColumnType("jsonb");

        // Financial fields
        builder.Property(e => e.NetWorthCurrent)
            .HasColumnName("net_worth_current")
            .HasPrecision(18, 2);

        builder.Property(e => e.NetWorthDelta)
            .HasColumnName("net_worth_delta")
            .HasPrecision(18, 2);

        builder.Property(e => e.TotalIncome)
            .HasColumnName("total_income")
            .HasPrecision(18, 2);

        builder.Property(e => e.TotalExpenses)
            .HasColumnName("total_expenses")
            .HasPrecision(18, 2);

        builder.Property(e => e.NetCashFlow)
            .HasColumnName("net_cash_flow")
            .HasPrecision(18, 2);

        builder.Property(e => e.SavingsRate)
            .HasColumnName("savings_rate")
            .HasPrecision(8, 4);

        builder.Property(e => e.DimensionScores)
            .HasColumnName("dimension_scores")
            .HasColumnType("jsonb");

        builder.Property(e => e.GeneratedAt)
            .HasColumnName("generated_at")
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        // Index for efficient querying
        builder.HasIndex(e => new { e.UserId, e.ReviewType, e.PeriodEnd })
            .IsDescending(false, false, true);

        builder.HasOne(e => e.User)
            .WithMany(u => u.ReviewSnapshots)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
