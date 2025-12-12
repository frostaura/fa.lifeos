using LifeOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Persistence.Configurations;

public class LongevitySnapshotConfiguration : IEntityTypeConfiguration<LongevitySnapshot>
{
    public void Configure(EntityTypeBuilder<LongevitySnapshot> builder)
    {
        builder.ToTable("longevity_snapshots");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.Timestamp)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.BaselineLifeExpectancyYears)
            .HasPrecision(4, 1)
            .IsRequired();

        builder.Property(e => e.AdjustedLifeExpectancyYears)
            .HasPrecision(4, 1)
            .IsRequired();

        builder.Property(e => e.TotalYearsAdded)
            .HasPrecision(4, 2)
            .IsRequired();

        builder.Property(e => e.RiskFactorCombined)
            .HasPrecision(5, 4)
            .IsRequired();

        builder.Property(e => e.Breakdown)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(e => e.Confidence)
            .HasMaxLength(20)
            .HasDefaultValue("medium");

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasOne(e => e.User)
            .WithMany(u => u.LongevitySnapshots)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.UserId, e.Timestamp });
    }
}
