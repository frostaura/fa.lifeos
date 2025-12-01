using LifeOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Persistence.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.AccountType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.Currency)
            .HasMaxLength(3)
            .HasDefaultValue("ZAR")
            .IsRequired();

        builder.Property(e => e.InitialBalance)
            .HasPrecision(18, 4)
            .HasDefaultValue(0m);

        builder.Property(e => e.CurrentBalance)
            .HasPrecision(18, 4)
            .HasDefaultValue(0m);

        builder.Property(e => e.BalanceUpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.Institution)
            .HasMaxLength(100);

        builder.Property(e => e.IsLiability)
            .HasDefaultValue(false);

        builder.Property(e => e.InterestRateAnnual)
            .HasPrecision(8, 5);

        builder.Property(e => e.InterestCompounding)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.MonthlyFee)
            .HasPrecision(18, 4)
            .HasDefaultValue(0m);

        builder.Property(e => e.Metadata)
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb");

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.UpdatedAt);

        builder.HasOne(e => e.User)
            .WithMany(u => u.Accounts)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => new { e.UserId, e.AccountType });
        builder.HasIndex(e => new { e.UserId, e.Currency });
        builder.HasIndex(e => e.UserId)
            .HasFilter("\"IsActive\" = TRUE")
            .HasDatabaseName("idx_accounts_active");
    }
}
