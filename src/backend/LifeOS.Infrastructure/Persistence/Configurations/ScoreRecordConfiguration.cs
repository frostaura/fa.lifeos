using LifeOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Persistence.Configurations;

public class ScoreRecordConfiguration : IEntityTypeConfiguration<ScoreRecord>
{
    public void Configure(EntityTypeBuilder<ScoreRecord> builder)
    {
        builder.ToTable("score_records");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.ScoreCode)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.ScoreValue)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(e => e.PeriodType)
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(e => e.Breakdown)
            .HasColumnType("jsonb");

        builder.Property(e => e.CalculatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasOne(e => e.User)
            .WithMany(u => u.ScoreRecords)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.ScoreDefinition)
            .WithMany(s => s.Records)
            .HasForeignKey(e => e.ScoreCode)
            .HasPrincipalKey(s => s.Code)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.UserId, e.ScoreCode, e.PeriodType, e.PeriodStart })
            .IsUnique();

        builder.HasIndex(e => new { e.UserId, e.CalculatedAt });
        builder.HasIndex(e => new { e.UserId, e.PeriodType, e.PeriodStart });
    }
}
