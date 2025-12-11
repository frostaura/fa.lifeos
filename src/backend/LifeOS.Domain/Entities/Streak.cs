using LifeOS.Domain.Common;

namespace LifeOS.Domain.Entities;

public class Streak : BaseEntity
{
    public Guid UserId { get; set; }
    
    public Guid? TaskId { get; set; }
    public string? MetricCode { get; set; }
    
    public int CurrentStreakLength { get; set; } = 0;
    public int LongestStreakLength { get; set; } = 0;
    public DateOnly? LastSuccessDate { get; set; }
    public DateOnly? StreakStartDate { get; set; }
    
    public int MissCount { get; set; } = 0;
    public int MaxAllowedMisses { get; set; } = 0;
    
    /// <summary>v1.1: Consecutive misses in a row (resets on success)</summary>
    public int ConsecutiveMisses { get; set; } = 0;
    
    /// <summary>
    /// v1.1: Accumulated risk penalty score
    /// Penalty formula: miss1 = forgiven, miss2 = +5, miss3+ = 10 * (misses-1)
    /// Decay: -2/day of compliance
    /// </summary>
    public decimal RiskPenaltyScore { get; set; } = 0;
    
    /// <summary>v1.1: When the penalty was last recalculated</summary>
    public DateTime? LastPenaltyCalculatedAt { get; set; }
    
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual LifeTask? Task { get; set; }
    public virtual MetricDefinition? MetricDefinition { get; set; }
}
