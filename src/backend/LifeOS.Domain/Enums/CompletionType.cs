namespace LifeOS.Domain.Enums;

/// <summary>
/// Type of task completion tracking method
/// v3.0 feature: Distinguishes manual vs auto-evaluated task completions
/// </summary>
public enum CompletionType
{
    /// <summary>User manually marked task as complete</summary>
    Manual = 0,
    
    /// <summary>Task auto-evaluated based on metric threshold</summary>
    AutoMetric = 1
}
