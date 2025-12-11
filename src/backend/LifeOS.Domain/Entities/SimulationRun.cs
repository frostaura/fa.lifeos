using LifeOS.Domain.Common;

namespace LifeOS.Domain.Entities;

/// <summary>
/// Records execution of a simulation scenario.
/// v1.2 feature: Tracks simulation runs with status and results
/// </summary>
public class SimulationRun : BaseEntity
{
    public Guid ScenarioId { get; set; }
    public Guid UserId { get; set; }
    
    /// <summary>When simulation started</summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>When simulation finished</summary>
    public DateTime? FinishedAt { get; set; }
    
    /// <summary>Status: success, failed, running</summary>
    public string Status { get; set; } = "running";
    
    /// <summary>
    /// JSONB summary of key milestones and results
    /// e.g., { "finalNetWorth": 1500000, "millionaireDate": "2035-03-15", "retirementReady": true }
    /// </summary>
    public string? Summary { get; set; }
    
    /// <summary>Error message if status is failed</summary>
    public string? ErrorMessage { get; set; }
    
    // Navigation properties
    public virtual SimulationScenario Scenario { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
