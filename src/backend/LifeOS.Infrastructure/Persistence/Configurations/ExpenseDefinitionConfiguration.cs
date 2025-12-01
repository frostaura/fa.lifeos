using LifeOS.Domain.Entities;
using LifeOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Persistence.Configurations;

public class ExpenseDefinitionConfiguration : IEntityTypeConfiguration<ExpenseDefinition>
{
    public void Configure(EntityTypeBuilder<ExpenseDefinition> builder)
    {
        builder.ToTable("expense_definitions");

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

        builder.Property(e => e.AmountType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.AmountValue)
            .HasPrecision(18, 4);

        builder.Property(e => e.AmountFormula);

        builder.Property(e => e.Frequency)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.Category)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.IsTaxDeductible)
            .HasDefaultValue(false);

        builder.Property(e => e.InflationAdjusted)
            .HasDefaultValue(true);

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.UpdatedAt);

        builder.HasOne(e => e.User)
            .WithMany(u => u.ExpenseDefinitions)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.LinkedAccount)
            .WithMany(a => a.ExpenseDefinitions)
            .HasForeignKey(e => e.LinkedAccountId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => new { e.UserId, e.Category });
        builder.HasIndex(e => e.UserId)
            .HasFilter("\"IsActive\" = TRUE")
            .HasDatabaseName("idx_expense_definitions_active");

        // Formula required if amount_type = 'formula'
        builder.ToTable(t => t.HasCheckConstraint("chk_expense_amount",
            "(\"AmountType\" = 'Formula' AND \"AmountFormula\" IS NOT NULL) OR (\"AmountType\" != 'Formula' AND \"AmountValue\" IS NOT NULL)"));
    }
}
