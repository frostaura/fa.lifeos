using LifeOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Persistence.Configurations;

public class ScoreDefinitionConfiguration : IEntityTypeConfiguration<ScoreDefinition>
{
    public void Configure(EntityTypeBuilder<ScoreDefinition> builder)
    {
        builder.ToTable("score_definitions");

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

        builder.Property(e => e.Formula)
            .IsRequired();

        builder.Property(e => e.FormulaVersion)
            .HasDefaultValue(1);

        builder.Property(e => e.MinScore)
            .HasPrecision(5, 2)
            .HasDefaultValue(0m);

        builder.Property(e => e.MaxScore)
            .HasPrecision(5, 2)
            .HasDefaultValue(100m);

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.UpdatedAt);

        builder.HasOne(e => e.Dimension)
            .WithMany(d => d.ScoreDefinitions)
            .HasForeignKey(e => e.DimensionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.Code)
            .IsUnique();

        builder.HasIndex(e => e.DimensionId);
    }
}
