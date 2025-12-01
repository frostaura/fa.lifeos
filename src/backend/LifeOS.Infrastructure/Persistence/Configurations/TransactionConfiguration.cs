using LifeOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Persistence.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(e => e.Amount)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(e => e.AmountHomeCurrency)
            .HasPrecision(18, 4);

        builder.Property(e => e.FxRateUsed)
            .HasPrecision(18, 8);

        builder.Property(e => e.Category)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(e => e.Subcategory)
            .HasMaxLength(50);

        builder.Property(e => e.Tags);

        builder.Property(e => e.Description)
            .HasMaxLength(255);

        builder.Property(e => e.Notes);

        builder.Property(e => e.RecordedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.Source)
            .HasMaxLength(50)
            .HasDefaultValue("manual");

        builder.Property(e => e.ExternalId)
            .HasMaxLength(100);

        builder.Property(e => e.IsReconciled)
            .HasDefaultValue(false);

        builder.Property(e => e.Metadata)
            .HasColumnType("jsonb");

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasOne(e => e.User)
            .WithMany(u => u.Transactions)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.SourceAccount)
            .WithMany(a => a.SourceTransactions)
            .HasForeignKey(e => e.SourceAccountId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.TargetAccount)
            .WithMany(a => a.TargetTransactions)
            .HasForeignKey(e => e.TargetAccountId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.SourceAccountId);
        builder.HasIndex(e => e.TargetAccountId);
        builder.HasIndex(e => new { e.UserId, e.TransactionDate });
        builder.HasIndex(e => new { e.UserId, e.Category });
        builder.HasIndex(e => e.Tags)
            .HasMethod("gin");

        // At least one account must be linked
        builder.ToTable(t => t.HasCheckConstraint("chk_transaction_accounts",
            "\"SourceAccountId\" IS NOT NULL OR \"TargetAccountId\" IS NOT NULL"));
    }
}
