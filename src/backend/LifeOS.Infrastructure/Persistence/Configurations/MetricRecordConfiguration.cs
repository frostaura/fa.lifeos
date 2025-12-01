using LifeOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Persistence.Configurations;

public class MetricRecordConfiguration : IEntityTypeConfiguration<MetricRecord>
{
    public void Configure(EntityTypeBuilder<MetricRecord> builder)
    {
        builder.ToTable("metric_records");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.MetricCode)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.ValueNumber)
            .HasPrecision(18, 4);

        builder.Property(e => e.ValueString)
            .HasMaxLength(255);

        builder.Property(e => e.RecordedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.Source)
            .HasMaxLength(50)
            .HasDefaultValue("manual");

        builder.Property(e => e.Notes);

        builder.Property(e => e.Metadata)
            .HasColumnType("jsonb");

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasOne(e => e.User)
            .WithMany(u => u.MetricRecords)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.MetricDefinition)
            .WithMany(m => m.Records)
            .HasForeignKey(e => e.MetricCode)
            .HasPrincipalKey(m => m.Code)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.UserId, e.MetricCode });
        builder.HasIndex(e => new { e.UserId, e.RecordedAt });
        builder.HasIndex(e => new { e.MetricCode, e.RecordedAt });
        builder.HasIndex(e => e.Source);
    }
}
