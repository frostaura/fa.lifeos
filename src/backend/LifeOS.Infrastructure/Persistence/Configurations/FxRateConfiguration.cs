using LifeOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Persistence.Configurations;

public class FxRateConfiguration : IEntityTypeConfiguration<FxRate>
{
    public void Configure(EntityTypeBuilder<FxRate> builder)
    {
        builder.ToTable("fx_rates");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.BaseCurrency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(e => e.QuoteCurrency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(e => e.Rate)
            .HasPrecision(18, 8)
            .IsRequired();

        builder.Property(e => e.RateTimestamp)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.Source)
            .HasMaxLength(50)
            .HasDefaultValue("coingecko");

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(e => new { e.BaseCurrency, e.QuoteCurrency, e.RateTimestamp })
            .IsUnique()
            .IsDescending(false, false, true);

        // Rate must be positive
        builder.ToTable(t => t.HasCheckConstraint("chk_fx_rate_positive", "\"Rate\" > 0"));
    }
}
