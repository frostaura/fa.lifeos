using LifeOS.Domain.Common;
using LifeOS.Domain.Enums;

namespace LifeOS.Domain.Entities;

public class MetricDefinition : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    public Guid? DimensionId { get; set; }
    public string? Unit { get; set; }
    public MetricValueType ValueType { get; set; } = MetricValueType.Number;
    public AggregationType AggregationType { get; set; } = AggregationType.Last;
    
    public string[]? EnumValues { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public decimal? TargetValue { get; set; }
    public TargetDirection TargetDirection { get; set; } = TargetDirection.AtOrAbove;
    
    public string? Icon { get; set; }
    public string[]? Tags { get; set; }
    public bool IsDerived { get; set; } = false;
    public string? DerivationFormula { get; set; }
    
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual Dimension? Dimension { get; set; }
    public virtual ICollection<MetricRecord> Records { get; set; } = new List<MetricRecord>();
    public virtual ICollection<Streak> Streaks { get; set; } = new List<Streak>();
}
