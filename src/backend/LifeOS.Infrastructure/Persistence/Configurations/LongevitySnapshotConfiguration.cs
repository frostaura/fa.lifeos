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

        builder.Property(e => e.CalculatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.BaselineLifeExpectancy)
            .HasPrecision(4, 1)
            .IsRequired();

        builder.Property(e => e.EstimatedYearsAdded)
            .HasPrecision(4, 1)
            .IsRequired();

        builder.Property(e => e.AdjustedLifeExpectancy)
            .HasPrecision(4, 1)
            .IsRequired();

        builder.Property(e => e.Breakdown)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(e => e.InputMetricsSnapshot)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(e => e.ConfidenceLevel)
            .HasMaxLength(20)
            .HasDefaultValue("moderate");

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasOne(e => e.User)
            .WithMany(u => u.LongevitySnapshots)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.UserId, e.CalculatedAt });
    }
}
