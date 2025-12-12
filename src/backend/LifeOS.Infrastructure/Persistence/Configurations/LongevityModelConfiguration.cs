using LifeOS.Domain.Entities;
using LifeOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Persistence.Configurations;

public class LongevityModelConfiguration : IEntityTypeConfiguration<LongevityModel>
{
    public void Configure(EntityTypeBuilder<LongevityModel> builder)
    {
        builder.ToTable("longevity_models");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.UserId);

        builder.Property(e => e.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Description);

        builder.Property(e => e.InputMetrics)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(e => e.ModelType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.Parameters)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(e => e.MaxRiskReduction)
            .HasPrecision(5, 4)
            .IsRequired();

        builder.Property(e => e.SourceCitation);

        builder.Property(e => e.SourceUrl);

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.UpdatedAt);

        builder.HasIndex(e => e.Code)
            .IsUnique();

        builder.HasIndex(e => e.UserId);
    }
}
