using LifeOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Persistence.Configurations;

public class TaxProfileConfiguration : IEntityTypeConfiguration<TaxProfile>
{
    public void Configure(EntityTypeBuilder<TaxProfile> builder)
    {
        builder.ToTable("tax_profiles");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.Name)
            .HasMaxLength(100)
            .HasDefaultValue("Default")
            .IsRequired();

        builder.Property(e => e.TaxYear)
            .IsRequired();

        builder.Property(e => e.CountryCode)
            .HasMaxLength(2)
            .HasDefaultValue("ZA")
            .IsRequired();

        builder.Property(e => e.Brackets)
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'[]'::jsonb");

        builder.Property(e => e.UifRate)
            .HasPrecision(5, 4)
            .HasDefaultValue(0.01m);

        builder.Property(e => e.UifCap)
            .HasPrecision(18, 4);

        builder.Property(e => e.VatRate)
            .HasPrecision(5, 4)
            .HasDefaultValue(0.15m);

        builder.Property(e => e.IsVatRegistered)
            .HasDefaultValue(false);

        builder.Property(e => e.TaxRebates)
            .HasColumnType("jsonb");

        builder.Property(e => e.MedicalCredits)
            .HasColumnType("jsonb");

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.UpdatedAt);

        builder.HasOne(e => e.User)
            .WithMany(u => u.TaxProfiles)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => new { e.UserId, e.TaxYear });
    }
}
