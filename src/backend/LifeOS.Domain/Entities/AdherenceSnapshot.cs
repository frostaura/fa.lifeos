using LifeOS.Domain.Common;

namespace LifeOS.Domain.Entities;

/// <summary>
/// Snapshot of Behavioral Adherence Index score (0-100).
/// v1.2 feature: Tracks task completion rate over time window
/// </summary>
public class AdherenceSnapshot : BaseEntity
{
    public Guid UserId { get; set; }
    
    /// <summary>When this snapshot was taken</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>Overall adherence score (0-100)</summary>
    public decimal Score { get; set; }
    
    /// <summary>Time window in days considered (e.g., 7 for weekly, 30 for monthly)</summary>
    public int TimeWindowDays { get; set; } = 7;
    
    /// <summary>Number of tasks considered in calculation</summary>
    public int TasksConsidered { get; set; }
    
    /// <summary>Number of tasks completed</summary>
    public int TasksCompleted { get; set; }
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
}
