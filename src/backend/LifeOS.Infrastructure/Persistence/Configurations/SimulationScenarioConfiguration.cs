using LifeOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Persistence.Configurations;

public class SimulationScenarioConfiguration : IEntityTypeConfiguration<SimulationScenario>
{
    public void Configure(EntityTypeBuilder<SimulationScenario> builder)
    {
        builder.ToTable("simulation_scenarios");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Description);

        builder.Property(e => e.EndCondition);

        builder.Property(e => e.BaseAssumptions)
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb");

        builder.Property(e => e.IsBaseline)
            .HasDefaultValue(false);

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.UpdatedAt);

        builder.HasOne(e => e.User)
            .WithMany(u => u.SimulationScenarios)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.UserId);

        // Only one baseline per user
        builder.HasIndex(e => e.UserId)
            .IsUnique()
            .HasFilter("\"IsBaseline\" = TRUE")
            .HasDatabaseName("idx_sim_scenarios_baseline");
    }
}
