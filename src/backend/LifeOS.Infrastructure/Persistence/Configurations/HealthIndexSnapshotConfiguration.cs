using LifeOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Persistence.Configurations;

public class HealthIndexSnapshotConfiguration : IEntityTypeConfiguration<HealthIndexSnapshot>
{
    public void Configure(EntityTypeBuilder<HealthIndexSnapshot> builder)
    {
        builder.ToTable("health_index_snapshots");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id");

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.Timestamp)
            .HasColumnName("timestamp");

        builder.Property(e => e.Score)
            .HasColumnName("score")
            .HasPrecision(5, 2);

        builder.Property(e => e.Components)
            .HasColumnName("components")
            .HasColumnType("jsonb");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasOne(e => e.User)
            .WithMany(u => u.HealthIndexSnapshots)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.UserId, e.Timestamp });
    }
}
