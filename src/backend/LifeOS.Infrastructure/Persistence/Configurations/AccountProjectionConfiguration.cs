using LifeOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Persistence.Configurations;

public class AccountProjectionConfiguration : IEntityTypeConfiguration<AccountProjection>
{
    public void Configure(EntityTypeBuilder<AccountProjection> builder)
    {
        builder.ToTable("account_projections");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.Balance)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(e => e.BalanceHomeCurrency)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(e => e.PeriodIncome)
            .HasPrecision(18, 4)
            .HasDefaultValue(0m);

        builder.Property(e => e.PeriodExpenses)
            .HasPrecision(18, 4)
            .HasDefaultValue(0m);

        builder.Property(e => e.PeriodInterest)
            .HasPrecision(18, 4)
            .HasDefaultValue(0m);

        builder.Property(e => e.PeriodTransfersIn)
            .HasPrecision(18, 4)
            .HasDefaultValue(0m);

        builder.Property(e => e.PeriodTransfersOut)
            .HasPrecision(18, 4)
            .HasDefaultValue(0m);

        builder.Property(e => e.FxRateUsed)
            .HasPrecision(18, 8);

        builder.Property(e => e.EventsApplied)
            .HasColumnType("jsonb");

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasOne(e => e.Scenario)
            .WithMany(s => s.AccountProjections)
            .HasForeignKey(e => e.ScenarioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Account)
            .WithMany(a => a.Projections)
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.ScenarioId, e.AccountId, e.PeriodDate })
            .IsUnique();

        builder.HasIndex(e => new { e.ScenarioId, e.PeriodDate });
        builder.HasIndex(e => new { e.AccountId, e.PeriodDate });
    }
}
