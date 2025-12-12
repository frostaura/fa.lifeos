using LifeOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Persistence.Configurations;

public class PrimaryStatConfiguration : IEntityTypeConfiguration<PrimaryStat>
{
    public void Configure(EntityTypeBuilder<PrimaryStat> builder)
    {
        builder.ToTable("primary_stats");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id");

        builder.Property(e => e.Code)
            .HasColumnName("code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasColumnName("description");

        builder.Property(e => e.Icon)
            .HasColumnName("icon")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.SortOrder)
            .HasColumnName("sort_order");

        builder.Property(e => e.IsActive)
            .HasColumnName("is_active");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasIndex(e => e.Code)
            .IsUnique();
    }
}
