using LifeOS.Domain.Common;

namespace LifeOS.Domain.Entities;

/// <summary>
/// Weekly or monthly review snapshot containing current values, deltas, and recommendations.
/// v1.1 feature: Weekly/Monthly Reviews
/// </summary>
public class ReviewSnapshot : BaseEntity
{
    public Guid UserId { get; set; }
    
    /// <summary>weekly or monthly</summary>
    public string ReviewType { get; set; } = "weekly";
    
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    
    /// <summary>Current Health Index value</summary>
    public decimal? HealthIndexCurrent { get; set; }
    
    /// <summary>Current Adherence Index value</summary>
    public decimal? AdherenceIndexCurrent { get; set; }
    
    /// <summary>Current Wealth Health value</summary>
    public decimal? WealthHealthCurrent { get; set; }
    
    /// <summary>Current Longevity value (years)</summary>
    public decimal? LongevityCurrent { get; set; }
    
    /// <summary>Change in Health Index from previous period</summary>
    public decimal? HealthIndexDelta { get; set; }
    
    /// <summary>Change in Behavioral Adherence Index from previous period</summary>
    public decimal? AdherenceIndexDelta { get; set; }
    
    /// <summary>Change in Wealth Health Score from previous period</summary>
    public decimal? WealthHealthDelta { get; set; }
    
    /// <summary>Change in Longevity Years Added from previous period</summary>
    public decimal? LongevityDelta { get; set; }
    
    /// <summary>
    /// Top streaks as JSON array [{taskId, streakDays, taskTitle}]
    /// </summary>
    public string? TopStreaks { get; set; }
    
    /// <summary>
    /// Recommended actions as JSON array [{action, priority, dimension}]
    /// </summary>
    public string? RecommendedActions { get; set; }
    
    /// <summary>
    /// Current primary stats as JSON object {strength: 50, wisdom: 60, ...}
    /// </summary>
    public string? PrimaryStatsCurrent { get; set; }
    
    /// <summary>
    /// Primary stats delta as JSON object {strength: +2, wisdom: -1, ...}
    /// </summary>
    public string? PrimaryStatsDelta { get; set; }
    
    /// <summary>
    /// Scenario comparison summary as JSON
    /// </summary>
    public string? ScenarioComparison { get; set; }
    
    // Financial Summary Fields
    /// <summary>Current net worth</summary>
    public decimal? NetWorthCurrent { get; set; }
    
    /// <summary>Net worth change from previous period</summary>
    public decimal? NetWorthDelta { get; set; }
    
    /// <summary>Total income in period</summary>
    public decimal? TotalIncome { get; set; }
    
    /// <summary>Total expenses in period</summary>
    public decimal? TotalExpenses { get; set; }
    
    /// <summary>Net cash flow (income - expenses)</summary>
    public decimal? NetCashFlow { get; set; }
    
    /// <summary>Savings rate as percentage</summary>
    public decimal? SavingsRate { get; set; }
    
    /// <summary>Dimension scores as JSON object</summary>
    public string? DimensionScores { get; set; }
    
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User User { get; set; } = null!;
}
