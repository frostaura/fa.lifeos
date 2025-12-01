using LifeOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Persistence.Configurations;

public class NetWorthProjectionConfiguration : IEntityTypeConfiguration<NetWorthProjection>
{
    public void Configure(EntityTypeBuilder<NetWorthProjection> builder)
    {
        builder.ToTable("net_worth_projections");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.TotalAssets)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(e => e.TotalLiabilities)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(e => e.NetWorth)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(e => e.BreakdownByType)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(e => e.BreakdownByCurrency)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(e => e.MilestonesReached)
            .HasColumnType("jsonb");

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasOne(e => e.Scenario)
            .WithMany(s => s.NetWorthProjections)
            .HasForeignKey(e => e.ScenarioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.ScenarioId, e.PeriodDate })
            .IsUnique();
    }
}
