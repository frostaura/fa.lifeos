using LifeOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Persistence.Configurations;

public class UserSettingsConfiguration : IEntityTypeConfiguration<UserSettings>
{
    public void Configure(EntityTypeBuilder<UserSettings> builder)
    {
        builder.ToTable("user_settings");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id");

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.HomeCurrency)
            .HasColumnName("home_currency")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(e => e.Timezone)
            .HasColumnName("timezone")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.BaselineLifeExpectancyYears)
            .HasColumnName("baseline_life_expectancy_years")
            .HasPrecision(5, 2);

        builder.Property(e => e.DefaultInflationRate)
            .HasColumnName("default_inflation_rate")
            .HasPrecision(8, 4);

        builder.Property(e => e.DefaultInvestmentGrowthRate)
            .HasColumnName("default_investment_growth_rate")
            .HasPrecision(8, 4);

        builder.Property(e => e.StreakPenaltySensitivity)
            .HasColumnName("streak_penalty_sensitivity")
            .HasMaxLength(20);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        // One user has one settings
        builder.HasIndex(e => e.UserId)
            .IsUnique();

        builder.HasOne(e => e.User)
            .WithOne(u => u.UserSettings)
            .HasForeignKey<UserSettings>(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
