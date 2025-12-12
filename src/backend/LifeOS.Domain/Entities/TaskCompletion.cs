using LifeOS.Domain.Common;
using LifeOS.Domain.Enums;

namespace LifeOS.Domain.Entities;

/// <summary>
/// Records when a task was completed by a user.
/// v1.2 feature: Historical tracking of task completion
/// v3.0 enhancement: Added CompletionType to distinguish manual vs auto-evaluated completions
/// </summary>
public class TaskCompletion : BaseEntity
{
    public Guid TaskId { get; set; }
    public Guid UserId { get; set; }
    
    /// <summary>When the task was completed</summary>
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Type of completion (Manual or AutoMetric)
    /// v3.0: Tracks whether user manually completed or system auto-evaluated based on metric
    /// </summary>
    public CompletionType CompletionType { get; set; } = CompletionType.Manual;
    
    /// <summary>Optional metric value recorded at completion</summary>
    public decimal? ValueNumber { get; set; }
    
    /// <summary>Optional notes about completion</summary>
    public string? Notes { get; set; }
    
    // Navigation properties
    public virtual LifeTask Task { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
