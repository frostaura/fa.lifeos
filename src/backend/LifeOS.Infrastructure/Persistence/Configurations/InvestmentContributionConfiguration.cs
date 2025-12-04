using LifeOS.Domain.Entities;
using LifeOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Persistence.Configurations;

public class InvestmentContributionConfiguration : IEntityTypeConfiguration<InvestmentContribution>
{
    public void Configure(EntityTypeBuilder<InvestmentContribution> builder)
    {
        builder.ToTable("InvestmentContributions");

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

        builder.Property(e => e.Amount)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(e => e.Frequency)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.Category)
            .HasMaxLength(50);

        builder.Property(e => e.AnnualIncreaseRate)
            .HasPrecision(5, 4);

        builder.Property(e => e.Notes);

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);

        // End condition properties
        builder.Property(e => e.EndConditionType)
            .HasConversion<string>()
            .HasMaxLength(30)
            .HasDefaultValue(EndConditionType.None);

        builder.Property(e => e.EndDate);

        builder.Property(e => e.EndAmountThreshold)
            .HasPrecision(18, 4);

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.UpdatedAt);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.TargetAccount)
            .WithMany()
            .HasForeignKey(e => e.TargetAccountId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.SourceAccount)
            .WithMany()
            .HasForeignKey(e => e.SourceAccountId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.EndConditionAccount)
            .WithMany()
            .HasForeignKey(e => e.EndConditionAccountId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.TargetAccountId);
        builder.HasIndex(e => e.SourceAccountId);
    }
}
