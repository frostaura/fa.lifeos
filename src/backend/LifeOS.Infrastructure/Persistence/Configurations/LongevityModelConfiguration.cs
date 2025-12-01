using LifeOS.Domain.Entities;
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

        builder.Property(e => e.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Description);

        builder.Property(e => e.InputMetrics)
            .IsRequired();

        builder.Property(e => e.ModelType)
            .HasMaxLength(50)
            .HasDefaultValue("linear")
            .IsRequired();

        builder.Property(e => e.Parameters)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(e => e.OutputUnit)
            .HasMaxLength(50)
            .HasDefaultValue("years_added")
            .IsRequired();

        builder.Property(e => e.SourceCitation);

        builder.Property(e => e.SourceUrl);

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);

        builder.Property(e => e.Version)
            .HasDefaultValue(1);

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.UpdatedAt);

        builder.HasIndex(e => e.Code)
            .IsUnique();

        builder.HasIndex(e => e.InputMetrics)
            .HasMethod("gin");
    }
}
