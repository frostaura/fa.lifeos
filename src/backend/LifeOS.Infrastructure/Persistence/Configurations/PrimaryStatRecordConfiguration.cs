using LifeOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Persistence.Configurations;

public class PrimaryStatRecordConfiguration : IEntityTypeConfiguration<PrimaryStatRecord>
{
    public void Configure(EntityTypeBuilder<PrimaryStatRecord> builder)
    {
        builder.ToTable("primary_stat_records");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id");

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.RecordedAt)
            .HasColumnName("recorded_at")
            .IsRequired();

        builder.Property(e => e.Strength)
            .HasColumnName("strength")
            .IsRequired();

        builder.Property(e => e.Wisdom)
            .HasColumnName("wisdom")
            .IsRequired();

        builder.Property(e => e.Charisma)
            .HasColumnName("charisma")
            .IsRequired();

        builder.Property(e => e.Composure)
            .HasColumnName("composure")
            .IsRequired();

        builder.Property(e => e.Energy)
            .HasColumnName("energy")
            .IsRequired();

        builder.Property(e => e.Influence)
            .HasColumnName("influence")
            .IsRequired();

        builder.Property(e => e.Vitality)
            .HasColumnName("vitality")
            .IsRequired();

        builder.Property(e => e.CalculationDetails)
            .HasColumnName("calculation_details")
            .HasColumnType("jsonb");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        // Index for efficient querying
        builder.HasIndex(e => new { e.UserId, e.RecordedAt })
            .IsDescending(false, true);

        builder.HasOne(e => e.User)
            .WithMany(u => u.PrimaryStatRecords)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
