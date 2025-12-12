namespace LifeOS.Application.DTOs.Mcp;

/// <summary>
/// MCP Tool: lifeos.listTasks
/// Lists tasks with optional filtering by dimension and completion status.
/// </summary>
public class ListTasksRequestDto
{
    /// <summary>Optional: Filter tasks by dimension code (e.g., "health_recovery")</summary>
    public string? DimensionCode { get; set; }
    
    /// <summary>Only include active (non-completed) tasks</summary>
    public bool OnlyActive { get; set; } = true;
    
    /// <summary>Include completed tasks in results</summary>
    public bool IncludeCompleted { get; set; } = false;
}

/// <summary>
/// Response containing list of tasks with streak information.
/// </summary>
public class ListTasksResponseDto
{
    /// <summary>List of tasks matching the filter criteria</summary>
    public List<TaskSummaryDto> Tasks { get; set; } = new();
}

/// <summary>
/// Summary of a single task with streak and completion information.
/// </summary>
public class TaskSummaryDto
{
    /// <summary>Task unique identifier</summary>
    public Guid Id { get; set; }
    
    /// <summary>Dimension code this task belongs to</summary>
    public string DimensionCode { get; set; } = string.Empty;
    
    /// <summary>Task title including emoji if present</summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>Emoji extracted from title (e.g., "🏋️‍♂️")</summary>
    public string? Emoji { get; set; }
    
    /// <summary>Task frequency (daily, weekly, monthly, etc.)</summary>
    public string Frequency { get; set; } = string.Empty;
    
    /// <summary>Whether this is a habit (recurring task)</summary>
    public bool IsHabit { get; set; }
    
    /// <summary>Last time this task was completed (ISO 8601)</summary>
    public DateTime? LastCompletedAt { get; set; }
    
    /// <summary>Streak information for this task</summary>
    public StreakInfoDto? Streak { get; set; }
    
    /// <summary>Optional: Linked metric code for auto-evaluation</summary>
    public string? LinkedMetricCode { get; set; }
    
    /// <summary>Optional: Target value for auto-evaluation</summary>
    public decimal? TargetValue { get; set; }
    
    /// <summary>Optional: Target comparison operator (atOrAbove, atOrBelow, range)</summary>
    public string? TargetComparison { get; set; }
}

/// <summary>
/// Streak information for a task.
/// </summary>
public class StreakInfoDto
{
    /// <summary>Current active streak length</summary>
    public int CurrentStreakLength { get; set; }
    
    /// <summary>Longest streak ever achieved</summary>
    public int LongestStreakLength { get; set; }
    
    /// <summary>Risk penalty score (0-100, based on consecutive misses)</summary>
    public decimal RiskPenaltyScore { get; set; }
    
    /// <summary>Number of consecutive misses</summary>
    public int ConsecutiveMisses { get; set; }
}
