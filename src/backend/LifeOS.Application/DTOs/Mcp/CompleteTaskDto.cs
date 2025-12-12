namespace LifeOS.Application.DTOs.Mcp;

/// <summary>
/// MCP Tool: lifeos.completeTask
/// Marks a task as completed and updates its streak.
/// </summary>
public class CompleteTaskRequestDto
{
    /// <summary>Task ID to complete</summary>
    public Guid TaskId { get; set; }
    
    /// <summary>
    /// Optional: Timestamp of completion (ISO 8601).
    /// Defaults to current time if not provided.
    /// </summary>
    public DateTime? Timestamp { get; set; }
    
    /// <summary>
    /// Optional: Metric value to record alongside completion.
    /// Used for metric-linked tasks (e.g., steps_count = 10500).
    /// </summary>
    public decimal? ValueNumber { get; set; }
}

/// <summary>
/// Response from completing a task via MCP tool.
/// </summary>
public class CompleteTaskResponseDto
{
    /// <summary>ID of the created TaskCompletion record</summary>
    public Guid TaskCompletionId { get; set; }
    
    /// <summary>Updated streak information after completion</summary>
    public UpdatedStreakDto UpdatedStreak { get; set; } = new();
    
    /// <summary>Whether a metric was recorded alongside completion</summary>
    public bool MetricRecorded { get; set; }
    
    /// <summary>ID of metric record if created</summary>
    public Guid? MetricRecordId { get; set; }
}

/// <summary>
/// Updated streak information after task completion.
/// </summary>
public class UpdatedStreakDto
{
    /// <summary>Current active streak length after this completion</summary>
    public int CurrentStreakLength { get; set; }
    
    /// <summary>Longest streak ever achieved</summary>
    public int LongestStreakLength { get; set; }
    
    /// <summary>Risk penalty score after this completion (0-100)</summary>
    public decimal RiskPenaltyScore { get; set; }
    
    /// <summary>Number of consecutive misses (should be 0 after success)</summary>
    public int ConsecutiveMisses { get; set; }
}
