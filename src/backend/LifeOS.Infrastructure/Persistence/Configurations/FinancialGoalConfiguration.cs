using LifeOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Persistence.Configurations;

public class FinancialGoalConfiguration : IEntityTypeConfiguration<FinancialGoal>
{
    public void Configure(EntityTypeBuilder<FinancialGoal> builder)
    {
        builder.ToTable("financial_goals");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.TargetAmount)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(e => e.CurrentAmount)
            .HasPrecision(18, 4)
            .HasDefaultValue(0m);

        builder.Property(e => e.Priority)
            .HasDefaultValue(1);

        builder.Property(e => e.TargetDate);

        builder.Property(e => e.Category)
            .HasMaxLength(100);

        builder.Property(e => e.IconName)
            .HasMaxLength(20);

        builder.Property(e => e.Currency)
            .HasMaxLength(3)
            .HasDefaultValue("ZAR")
            .IsRequired();

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);

        builder.Property(e => e.Notes)
            .HasMaxLength(1000);

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.UpdatedAt);

        // Ignore computed properties
        builder.Ignore(e => e.RemainingAmount);
        builder.Ignore(e => e.ProgressPercent);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => new { e.UserId, e.Priority });
        builder.HasIndex(e => e.UserId)
            .HasFilter("\"IsActive\" = TRUE")
            .HasDatabaseName("idx_financial_goals_active");
    }
}
