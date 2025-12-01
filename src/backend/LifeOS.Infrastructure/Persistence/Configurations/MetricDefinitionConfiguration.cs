using LifeOS.Domain.Entities;
using LifeOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Persistence.Configurations;

public class MetricDefinitionConfiguration : IEntityTypeConfiguration<MetricDefinition>
{
    public void Configure(EntityTypeBuilder<MetricDefinition> builder)
    {
        builder.ToTable("metric_definitions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Description);

        builder.Property(e => e.Unit)
            .HasMaxLength(20);

        builder.Property(e => e.ValueType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.AggregationType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.EnumValues);

        builder.Property(e => e.MinValue)
            .HasPrecision(18, 4);

        builder.Property(e => e.MaxValue)
            .HasPrecision(18, 4);

        builder.Property(e => e.TargetValue)
            .HasPrecision(18, 4);

        builder.Property(e => e.Icon)
            .HasMaxLength(50);

        builder.Property(e => e.Tags);

        builder.Property(e => e.IsDerived)
            .HasDefaultValue(false);

        builder.Property(e => e.DerivationFormula);

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.UpdatedAt);

        builder.HasOne(e => e.Dimension)
            .WithMany(d => d.MetricDefinitions)
            .HasForeignKey(e => e.DimensionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.Code)
            .IsUnique();

        builder.HasIndex(e => e.DimensionId);

        builder.HasIndex(e => e.Tags)
            .HasMethod("gin");
    }
}
