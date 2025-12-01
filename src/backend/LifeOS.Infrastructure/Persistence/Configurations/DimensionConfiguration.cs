using LifeOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Persistence.Configurations;

public class DimensionConfiguration : IEntityTypeConfiguration<Dimension>
{
    public void Configure(EntityTypeBuilder<Dimension> builder)
    {
        builder.ToTable("dimensions");

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

        builder.Property(e => e.Icon)
            .HasMaxLength(50);

        builder.Property(e => e.DefaultWeight)
            .HasPrecision(3, 2)
            .HasDefaultValue(0.125m);

        builder.Property(e => e.SortOrder)
            .HasDefaultValue((short)0);

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.UpdatedAt);

        builder.HasIndex(e => e.Code)
            .IsUnique();

        builder.HasIndex(e => e.IsActive)
            .HasFilter("\"IsActive\" = TRUE");
    }
}
