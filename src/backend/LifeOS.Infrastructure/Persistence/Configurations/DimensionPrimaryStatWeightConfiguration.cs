using LifeOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Persistence.Configurations;

public class DimensionPrimaryStatWeightConfiguration : IEntityTypeConfiguration<DimensionPrimaryStatWeight>
{
    public void Configure(EntityTypeBuilder<DimensionPrimaryStatWeight> builder)
    {
        builder.ToTable("dimension_primary_stat_weights");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id");

        builder.Property(e => e.DimensionId)
            .HasColumnName("dimension_id")
            .IsRequired();

        builder.Property(e => e.PrimaryStatCode)
            .HasColumnName("primary_stat_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Weight)
            .HasColumnName("weight")
            .HasPrecision(5, 4);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasOne(e => e.Dimension)
            .WithMany(d => d.PrimaryStatWeights)
            .HasForeignKey(e => e.DimensionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.PrimaryStat)
            .WithMany(ps => ps.DimensionWeights)
            .HasForeignKey(e => e.PrimaryStatCode)
            .HasPrincipalKey(ps => ps.Code)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.DimensionId, e.PrimaryStatCode })
            .IsUnique();
    }
}
