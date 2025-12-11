using LifeOS.Domain.Common;

namespace LifeOS.Domain.Entities;

public class SimulationScenario : BaseEntity
{
    public Guid UserId { get; set; }
    
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? EndDate { get; set; }
    public string? EndCondition { get; set; }
    
    public string BaseAssumptions { get; set; } = "{}";
    
    public bool IsBaseline { get; set; } = false;
    public DateTime? LastRunAt { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual ICollection<SimulationEvent> Events { get; set; } = new List<SimulationEvent>();
    public virtual ICollection<AccountProjection> AccountProjections { get; set; } = new List<AccountProjection>();
    public virtual ICollection<NetWorthProjection> NetWorthProjections { get; set; } = new List<NetWorthProjection>();
    
    // v1.2 Navigation properties
    public virtual ICollection<SimulationRun> SimulationRuns { get; set; } = new List<SimulationRun>();
}
