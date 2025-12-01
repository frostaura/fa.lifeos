using LifeOS.Domain.Entities;
using LifeOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Persistence.Configurations;

public class SimulationEventConfiguration : IEntityTypeConfiguration<SimulationEvent>
{
    public void Configure(EntityTypeBuilder<SimulationEvent> builder)
    {
        builder.ToTable("simulation_events");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Description);

        builder.Property(e => e.TriggerType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.TriggerCondition);

        builder.Property(e => e.EventType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Currency)
            .HasMaxLength(3);

        builder.Property(e => e.AmountType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.AmountValue)
            .HasPrecision(18, 4);

        builder.Property(e => e.AmountFormula);

        builder.Property(e => e.AppliesOnce)
            .HasDefaultValue(true);

        builder.Property(e => e.RecurrenceFrequency)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.SortOrder)
            .HasDefaultValue(0);

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.UpdatedAt);

        builder.HasOne(e => e.Scenario)
            .WithMany(s => s.Events)
            .HasForeignKey(e => e.ScenarioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.AffectedAccount)
            .WithMany(a => a.SimulationEvents)
            .HasForeignKey(e => e.AffectedAccountId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.ScenarioId);
        builder.HasIndex(e => new { e.TriggerType, e.TriggerDate });
    }
}
