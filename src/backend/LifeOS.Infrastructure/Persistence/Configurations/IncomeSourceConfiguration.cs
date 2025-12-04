using LifeOS.Domain.Entities;
using LifeOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Persistence.Configurations;

public class IncomeSourceConfiguration : IEntityTypeConfiguration<IncomeSource>
{
    public void Configure(EntityTypeBuilder<IncomeSource> builder)
    {
        builder.ToTable("income_sources");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Currency)
            .HasMaxLength(3)
            .HasDefaultValue("ZAR")
            .IsRequired();

        builder.Property(e => e.BaseAmount)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(e => e.IsPreTax)
            .HasDefaultValue(true);

        builder.Property(e => e.PaymentFrequency)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.AnnualIncreaseRate)
            .HasPrecision(5, 4)
            .HasDefaultValue(0.05m);

        builder.Property(e => e.EmployerName)
            .HasMaxLength(100);

        builder.Property(e => e.Notes);

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.UpdatedAt);

        builder.HasOne(e => e.User)
            .WithMany(u => u.IncomeSources)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.TaxProfile)
            .WithMany(t => t.IncomeSources)
            .HasForeignKey(e => e.TaxProfileId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.TargetAccount)
            .WithMany()
            .HasForeignKey(e => e.TargetAccountId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.UserId)
            .HasFilter("\"IsActive\" = TRUE")
            .HasDatabaseName("idx_income_sources_active");
    }
}
