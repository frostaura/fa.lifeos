using LifeOS.Domain.Common;
using LifeOS.Domain.Enums;

namespace LifeOS.Domain.Entities;

public class SimulationEvent : BaseEntity
{
    public Guid ScenarioId { get; set; }
    
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    public SimTriggerType TriggerType { get; set; } = SimTriggerType.Date;
    public DateOnly? TriggerDate { get; set; }
    public short? TriggerAge { get; set; }
    public string? TriggerCondition { get; set; }
    
    public string EventType { get; set; } = string.Empty;
    public string? Currency { get; set; }
    public AmountType AmountType { get; set; } = AmountType.Fixed;
    public decimal? AmountValue { get; set; }
    public string? AmountFormula { get; set; }
    
    /// <summary>Legacy: Single affected account (deprecated in v1.1)</summary>
    public Guid? AffectedAccountId { get; set; }
    
    /// <summary>v1.1: Source account for transfers/debits</summary>
    public Guid? SourceAccountId { get; set; }
    
    /// <summary>v1.1: Target account for transfers/credits</summary>
    public Guid? TargetAccountId { get; set; }
    
    public bool AppliesOnce { get; set; } = true;
    public PaymentFrequency? RecurrenceFrequency { get; set; }
    public DateOnly? RecurrenceEndDate { get; set; }
    
    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual SimulationScenario Scenario { get; set; } = null!;
    public virtual Account? AffectedAccount { get; set; }
    public virtual Account? SourceAccount { get; set; }
    public virtual Account? TargetAccount { get; set; }
}
