namespace LifeOS.Application.DTOs.Mcp;

/// <summary>
/// Request DTO for monthly review MCP tool.
/// </summary>
public class MonthlyReviewRequestDto
{
    /// <summary>
    /// Optional month in "YYYY-MM" format or "MM" format. Defaults to current month if not provided.
    /// </summary>
    public string? Month { get; set; }
    
    /// <summary>
    /// Optional year. Defaults to current year if not provided.
    /// </summary>
    public int? Year { get; set; }
}

/// <summary>
/// Response DTO for monthly review containing score trends, net worth, identity radar, and milestones.
/// </summary>
public class MonthlyReviewDto
{
    /// <summary>Month period (start and end dates)</summary>
    public PeriodDto Period { get; set; } = new();
    
    /// <summary>Overall LifeOS Score change from month start to end</summary>
    public ScoreChangeDto? LifeScoreChange { get; set; }
    
    /// <summary>Net worth change from month start to end</summary>
    public NetWorthChangeDto? NetWorthChange { get; set; }
    
    /// <summary>Longevity years added change from month start to end</summary>
    public LongevityChangeDto? LongevityChange { get; set; }
    
    /// <summary>Identity radar comparison (current vs targets)</summary>
    public IdentityRadarComparisonDto? IdentityRadarComparison { get; set; }
    
    /// <summary>Active milestone progress</summary>
    public List<MilestoneProgressDto> MilestoneProgress { get; set; } = new();
    
    /// <summary>Top wins/achievements this month</summary>
    public List<string> TopWins { get; set; } = new();
}

/// <summary>
/// Net worth change from period start to end.
/// </summary>
public class NetWorthChangeDto
{
    /// <summary>Net worth at period start</summary>
    public decimal From { get; set; }
    
    /// <summary>Net worth at period end</summary>
    public decimal To { get; set; }
}

/// <summary>
/// Identity radar comparison showing current stats vs targets.
/// </summary>
public class IdentityRadarComparisonDto
{
    /// <summary>Current primary stat values</summary>
    public PrimaryStatsDto Current { get; set; } = new();
    
    /// <summary>Target primary stat values from identity profile</summary>
    public PrimaryStatsDto Targets { get; set; } = new();
}

/// <summary>
/// Primary stats (7 stats: strength, wisdom, charisma, composure, energy, influence, vitality).
/// </summary>
public class PrimaryStatsDto
{
    /// <summary>Strength stat (0-100)</summary>
    public int Strength { get; set; }
    
    /// <summary>Wisdom stat (0-100)</summary>
    public int Wisdom { get; set; }
    
    /// <summary>Charisma stat (0-100)</summary>
    public int Charisma { get; set; }
    
    /// <summary>Composure stat (0-100)</summary>
    public int Composure { get; set; }
    
    /// <summary>Energy stat (0-100)</summary>
    public int Energy { get; set; }
    
    /// <summary>Influence stat (0-100)</summary>
    public int Influence { get; set; }
    
    /// <summary>Vitality stat (0-100)</summary>
    public int Vitality { get; set; }
}

/// <summary>
/// Milestone progress entry.
/// </summary>
public class MilestoneProgressDto
{
    /// <summary>Milestone title</summary>
    public string MilestoneTitle { get; set; } = string.Empty;
    
    /// <summary>Percent complete (0-100)</summary>
    public int PercentComplete { get; set; }
    
    /// <summary>Target date (ISO 8601 format: yyyy-MM-dd)</summary>
    public string? TargetDate { get; set; }
}
