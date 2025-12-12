namespace LifeOS.Application.DTOs.Mcp;

/// <summary>
/// Request DTO for weekly review MCP tool.
/// </summary>
public class WeeklyReviewRequestDto
{
    /// <summary>
    /// Optional week start date (Monday). Defaults to current week if not provided.
    /// Format: yyyy-MM-dd
    /// </summary>
    public DateTime? WeekStartDate { get; set; }
}

/// <summary>
/// Response DTO for weekly review containing score changes, streaks, and focus actions.
/// </summary>
public class WeeklyReviewDto
{
    /// <summary>Week period (start and end dates)</summary>
    public PeriodDto Period { get; set; } = new();
    
    /// <summary>Health Index change from week start to end</summary>
    public ScoreChangeDto? HealthIndexChange { get; set; }
    
    /// <summary>Adherence Index change from week start to end</summary>
    public ScoreChangeDto? AdherenceIndexChange { get; set; }
    
    /// <summary>Wealth Health Score change from week start to end</summary>
    public ScoreChangeDto? WealthHealthChange { get; set; }
    
    /// <summary>Longevity years added change from week start to end</summary>
    public LongevityChangeDto? LongevityChange { get; set; }
    
    /// <summary>Top 3 longest active streaks</summary>
    public List<TopStreakDto> TopStreaks { get; set; } = new();
    
    /// <summary>Streaks at risk of breaking (consecutive misses >= 2)</summary>
    public List<AtRiskStreakDto> AtRiskStreaks { get; set; } = new();
    
    /// <summary>AI-generated focus actions for next week</summary>
    public List<string> FocusActions { get; set; } = new();
}

/// <summary>
/// Period with start and end dates.
/// </summary>
public class PeriodDto
{
    /// <summary>Period start date (ISO 8601 format: yyyy-MM-dd)</summary>
    public string Start { get; set; } = string.Empty;
    
    /// <summary>Period end date (ISO 8601 format: yyyy-MM-dd)</summary>
    public string End { get; set; } = string.Empty;
}

/// <summary>
/// Score change showing from and to values.
/// </summary>
public class ScoreChangeDto
{
    /// <summary>Score at period start</summary>
    public decimal From { get; set; }
    
    /// <summary>Score at period end</summary>
    public decimal To { get; set; }
}

/// <summary>
/// Longevity years added change.
/// </summary>
public class LongevityChangeDto
{
    /// <summary>Longevity years added at period start</summary>
    public decimal From { get; set; }
    
    /// <summary>Longevity years added at period end</summary>
    public decimal To { get; set; }
}

/// <summary>
/// Top performing streak.
/// </summary>
public class TopStreakDto
{
    /// <summary>Task title</summary>
    public string TaskTitle { get; set; } = string.Empty;
    
    /// <summary>Current streak length (days)</summary>
    public int StreakLength { get; set; }
    
    /// <summary>Dimension code this task belongs to</summary>
    public string DimensionCode { get; set; } = string.Empty;
}

/// <summary>
/// Streak at risk of breaking.
/// </summary>
public class AtRiskStreakDto
{
    /// <summary>Task title</summary>
    public string TaskTitle { get; set; } = string.Empty;
    
    /// <summary>Consecutive misses count</summary>
    public int ConsecutiveMisses { get; set; }
    
    /// <summary>Penalty score incurred (5 on 2nd miss, 10×(n-1) after)</summary>
    public int RiskPenaltyScore { get; set; }
}
