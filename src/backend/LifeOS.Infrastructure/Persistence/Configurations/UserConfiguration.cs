using LifeOS.Domain.Entities;
using LifeOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.Email)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.Username)
            .HasMaxLength(50);

        builder.Property(e => e.PasswordHash)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.HomeCurrency)
            .HasMaxLength(3)
            .HasDefaultValue("ZAR")
            .IsRequired();

        builder.Property(e => e.LifeExpectancyBaseline)
            .HasPrecision(4, 1)
            .HasDefaultValue(80);

        builder.Property(e => e.DefaultAssumptions)
            .HasColumnType("jsonb")
            .HasDefaultValueSql(@"'{""inflationRateAnnual"": 0.05, ""defaultGrowthRate"": 0.07, ""retirementAge"": 65}'::jsonb");

        builder.Property(e => e.Role)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.UpdatedAt);

        builder.HasIndex(e => e.Email)
            .IsUnique();

        builder.HasIndex(e => e.Username)
            .IsUnique()
            .HasFilter("\"Username\" IS NOT NULL");
    }
}
