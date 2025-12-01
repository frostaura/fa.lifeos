using LifeOS.Domain.Common;

namespace LifeOS.Domain.Entities;

public class NetWorthProjection : BaseEntity
{
    public Guid ScenarioId { get; set; }
    
    public DateOnly PeriodDate { get; set; }
    
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal NetWorth { get; set; }
    
    public string BreakdownByType { get; set; } = "{}";
    public string BreakdownByCurrency { get; set; } = "{}";
    public string? MilestonesReached { get; set; }

    // Navigation properties
    public virtual SimulationScenario Scenario { get; set; } = null!;
}
