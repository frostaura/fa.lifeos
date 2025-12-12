using System.ComponentModel;

namespace LifeOS.Application.DTOs.Mcp;

#region List Tasks

/// <summary>
/// Request to list tasks with filtering options.
/// </summary>
public class ListTasksRequest
{
    [Description("Filter tasks by dimension code (e.g., 'health', 'wealth', 'wisdom')")]
    public string DimensionCode { get; set; } = string.Empty;

    [Description("Filter by task type: 'habit', 'todo', or 'project'")]
    public string TaskType { get; set; } = string.Empty;

    [Description("When true, only returns active (non-archived) tasks")]
    public bool OnlyActive { get; set; } = true;

    [Description("When true, includes tasks marked as completed")]
    public bool IncludeCompleted { get; set; } = false;
}

/// <summary>
/// Response containing list of tasks.
/// </summary>
public class ListTasksResponse
{
    [Description("Array of tasks matching the filter criteria")]
    public List<TaskSummary> Tasks { get; set; } = new();

    [Description("Total number of tasks returned")]
    public int TotalCount { get; set; }
}

/// <summary>
/// Summary of a task for list views.
/// </summary>
public class TaskSummary
{
    [Description("Unique identifier for the task")]
    public Guid Id { get; set; }

    [Description("Task title/name")]
    public string Title { get; set; } = string.Empty;

    [Description("Task description")]
    public string Description { get; set; } = string.Empty;

    [Description("Dimension code this task belongs to")]
    public string DimensionCode { get; set; } = string.Empty;

    [Description("Task type: 'habit', 'todo', or 'project'")]
    public string TaskType { get; set; } = string.Empty;

    [Description("Frequency for habits: 'daily', 'weekly', 'monthly'")]
    public string Frequency { get; set; } = string.Empty;

    [Description("Whether the task is currently active")]
    public bool IsActive { get; set; }

    [Description("Whether the task is completed")]
    public bool IsCompleted { get; set; }

    [Description("Current streak length for habit tasks")]
    public int CurrentStreak { get; set; }

    [Description("Longest streak ever achieved")]
    public int LongestStreak { get; set; }

    [Description("Last completion timestamp")]
    public DateTime? LastCompletedAt { get; set; }

    [Description("Linked metric code for auto-evaluation")]
    public string LinkedMetricCode { get; set; } = string.Empty;

    [Description("Target value for metric-linked tasks")]
    public decimal? TargetValue { get; set; }

    [Description("Tags assigned to this task")]
    public List<string> Tags { get; set; } = new();
}

#endregion

#region Get Task

/// <summary>
/// Request to get a single task by ID.
/// </summary>
public class GetTaskRequest
{
    [Description("The unique identifier of the task to retrieve")]
    public Guid TaskId { get; set; }
}

/// <summary>
/// Detailed response for a single task.
/// </summary>
public class GetTaskResponse
{
    [Description("The requested task details")]
    public TaskDetail Task { get; set; } = new();
}

/// <summary>
/// Full task details including all fields.
/// </summary>
public class TaskDetail
{
    [Description("Unique identifier for the task")]
    public Guid Id { get; set; }

    [Description("Task title/name")]
    public string Title { get; set; } = string.Empty;

    [Description("Detailed task description")]
    public string Description { get; set; } = string.Empty;

    [Description("Task type: 'habit', 'todo', or 'project'")]
    public string TaskType { get; set; } = string.Empty;

    [Description("Frequency for habits: 'daily', 'weekly', 'monthly'")]
    public string Frequency { get; set; } = string.Empty;

    [Description("Dimension ID this task belongs to")]
    public Guid? DimensionId { get; set; }

    [Description("Dimension code this task belongs to")]
    public string DimensionCode { get; set; } = string.Empty;

    [Description("Milestone ID this task contributes to")]
    public Guid? MilestoneId { get; set; }

    [Description("Linked metric code for auto-evaluation")]
    public string LinkedMetricCode { get; set; } = string.Empty;

    [Description("Target value for metric-linked tasks")]
    public decimal? TargetValue { get; set; }

    [Description("Target comparison: 'atOrAbove', 'atOrBelow', 'range'")]
    public string TargetComparison { get; set; } = string.Empty;

    [Description("Scheduled date for todo tasks")]
    public DateTime? ScheduledDate { get; set; }

    [Description("Scheduled time for todo tasks")]
    public TimeSpan? ScheduledTime { get; set; }

    [Description("Start date for recurring tasks")]
    public DateTime? StartDate { get; set; }

    [Description("End date for recurring tasks")]
    public DateTime? EndDate { get; set; }

    [Description("Whether the task is currently active")]
    public bool IsActive { get; set; }

    [Description("Whether the task is completed")]
    public bool IsCompleted { get; set; }

    [Description("Tags assigned to this task")]
    public List<string> Tags { get; set; } = new();

    [Description("Streak information for habit tasks")]
    public StreakInfo Streak { get; set; } = new();

    [Description("When the task was created")]
    public DateTime CreatedAt { get; set; }

    [Description("When the task was last updated")]
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Streak information for a task.
/// </summary>
public class StreakInfo
{
    [Description("Current active streak length")]
    public int CurrentLength { get; set; }

    [Description("Longest streak ever achieved")]
    public int LongestLength { get; set; }

    [Description("Risk penalty score (0-100)")]
    public decimal RiskPenalty { get; set; }

    [Description("Number of consecutive misses")]
    public int ConsecutiveMisses { get; set; }
}

#endregion

#region Create Task

/// <summary>
/// Request to create a new task.
/// </summary>
public class CreateTaskRequest
{
    [Description("Task title/name (required)")]
    public string Title { get; set; } = string.Empty;

    [Description("Detailed task description")]
    public string Description { get; set; } = string.Empty;

    [Description("Task type: 'habit', 'todo', or 'project' (required)")]
    public string TaskType { get; set; } = string.Empty;

    [Description("Frequency for habits: 'daily', 'weekly', 'monthly'")]
    public string Frequency { get; set; } = "daily";

    [Description("Dimension ID to assign this task to")]
    public Guid? DimensionId { get; set; }

    [Description("Milestone ID this task contributes to")]
    public Guid? MilestoneId { get; set; }

    [Description("Linked metric code for auto-evaluation")]
    public string LinkedMetricCode { get; set; } = string.Empty;

    [Description("Scheduled date for todo tasks (YYYY-MM-DD format)")]
    public DateTime? ScheduledDate { get; set; }

    [Description("Tags to assign to this task")]
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Response after creating a task.
/// </summary>
public class CreateTaskResponse
{
    [Description("The ID of the newly created task")]
    public Guid TaskId { get; set; }

    [Description("The created task details")]
    public TaskDetail Task { get; set; } = new();
}

#endregion

#region Update Task

/// <summary>
/// Request to update an existing task.
/// </summary>
public class UpdateTaskRequest
{
    [Description("The unique identifier of the task to update")]
    public Guid TaskId { get; set; }

    [Description("New task title (leave empty to keep current)")]
    public string Title { get; set; } = string.Empty;

    [Description("New task description (leave empty to keep current)")]
    public string Description { get; set; } = string.Empty;

    [Description("New frequency for habits: 'daily', 'weekly', 'monthly'")]
    public string Frequency { get; set; } = string.Empty;

    [Description("New scheduled date for todo tasks")]
    public DateTime? ScheduledDate { get; set; }

    [Description("New end date for recurring tasks")]
    public DateTime? EndDate { get; set; }

    [Description("Set to false to archive the task")]
    public bool IsActive { get; set; } = true;

    [Description("New tags to assign (replaces existing)")]
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Response after updating a task.
/// </summary>
public class UpdateTaskResponse
{
    [Description("Whether the update was successful")]
    public bool Success { get; set; }

    [Description("The updated task details")]
    public TaskDetail Task { get; set; } = new();
}

#endregion

#region Delete Task

/// <summary>
/// Request to delete a task.
/// </summary>
public class DeleteTaskRequest
{
    [Description("The unique identifier of the task to delete")]
    public Guid TaskId { get; set; }
}

/// <summary>
/// Response after deleting a task.
/// </summary>
public class DeleteTaskResponse
{
    [Description("Whether the deletion was successful")]
    public bool Success { get; set; }

    [Description("ID of the deleted task")]
    public Guid DeletedTaskId { get; set; }
}

#endregion

#region Complete Task

/// <summary>
/// Request to mark a task as complete.
/// </summary>
public class CompleteTaskRequest
{
    [Description("The unique identifier of the task to complete")]
    public Guid TaskId { get; set; }

    [Description("Metric value for metric-linked tasks (e.g., steps count)")]
    public decimal? MetricValue { get; set; }
}

/// <summary>
/// Response after completing a task.
/// </summary>
public class CompleteTaskResponse
{
    [Description("Whether the completion was successful")]
    public bool Success { get; set; }

    [Description("ID of the task completion record")]
    public Guid CompletionId { get; set; }

    [Description("Updated streak information")]
    public StreakInfo UpdatedStreak { get; set; } = new();

    [Description("Whether an associated metric was recorded")]
    public bool MetricRecorded { get; set; }

    [Description("ID of the metric record if created")]
    public Guid? MetricRecordId { get; set; }
}

#endregion
