namespace LifeOS.Application.DTOs.Mcp;

/// <summary>
/// Complete dashboard snapshot for AI integration via MCP tools.
/// Provides comprehensive LifeOS state in a single call.
/// </summary>
public class DashboardSnapshotDto
{
    /// <summary>Overall LifeOS Score (0-100)</summary>
    public decimal LifeScore { get; set; }
    
    /// <summary>Health Index component (0-100)</summary>
    public decimal HealthIndex { get; set; }
    
    /// <summary>Behavioral Adherence Index (0-100)</summary>
    public decimal AdherenceIndex { get; set; }
    
    /// <summary>Wealth Health Score (0-100)</summary>
    public decimal WealthHealthScore { get; set; }
    
    /// <summary>Longevity years added (0-20)</summary>
    public decimal LongevityYearsAdded { get; set; }
    
    /// <summary>Primary stats breakdown (strength, wisdom, etc.)</summary>
    public Dictionary<string, int> PrimaryStats { get; set; } = new();
    
    /// <summary>Dimension scores (8 dimensions)</summary>
    public List<DimensionScoreDto> Dimensions { get; set; } = new();
    
    /// <summary>Today's tasks (daily + scheduled for today)</summary>
    public List<TodayTaskDto> TodayTasks { get; set; } = new();
    
    /// <summary>Net worth in home currency</summary>
    public decimal? NetWorthHomeCcy { get; set; }
    
    /// <summary>Upcoming key events (weekly review, etc.)</summary>
    public List<KeyEventDto> NextKeyEvents { get; set; } = new();
}

/// <summary>
/// Dimension score entry for dashboard snapshot.
/// </summary>
public class DimensionScoreDto
{
    /// <summary>Dimension code (e.g., "health_recovery")</summary>
    public string Code { get; set; } = string.Empty;
    
    /// <summary>Dimension score (0-100)</summary>
    public decimal Score { get; set; }
}

/// <summary>
/// Today's task entry for dashboard snapshot.
/// </summary>
public class TodayTaskDto
{
    /// <summary>Task ID</summary>
    public Guid TaskId { get; set; }
    
    /// <summary>Dimension code this task belongs to</summary>
    public string DimensionCode { get; set; } = string.Empty;
    
    /// <summary>Task title</summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>Whether task is completed today</summary>
    public bool IsCompleted { get; set; }
    
    /// <summary>Task frequency (daily, weekly, etc.)</summary>
    public string Frequency { get; set; } = string.Empty;
}

/// <summary>
/// Key event entry for dashboard snapshot.
/// </summary>
public class KeyEventDto
{
    /// <summary>Event type (e.g., "weekly_review_due")</summary>
    public string Type { get; set; } = string.Empty;
    
    /// <summary>Event date (ISO 8601 format)</summary>
    public string Date { get; set; } = string.Empty;
}
