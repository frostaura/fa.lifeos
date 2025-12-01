namespace LifeOS.Application.DTOs.Tasks;

public record TaskDto
{
    public Guid Id { get; init; }
    public Guid? DimensionId { get; init; }
    public string? DimensionCode { get; init; }
    public Guid? MilestoneId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string TaskType { get; init; } = string.Empty;
    public string Frequency { get; init; } = string.Empty;
    public DateOnly? ScheduledDate { get; init; }
    public TimeOnly? ScheduledTime { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public string? LinkedMetricCode { get; init; }
    public bool IsCompleted { get; init; }
    public DateTime? CompletedAt { get; init; }
    public bool IsActive { get; init; }
    public string[]? Tags { get; init; }
    public int CurrentStreak { get; init; }
    public int LongestStreak { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record TaskListResponse
{
    public List<TaskItemResponse> Data { get; init; } = new();
    public PaginationMeta? Meta { get; init; }
}

public record TaskItemResponse
{
    public Guid Id { get; init; }
    public string Type { get; init; } = "task";
    public TaskAttributes Attributes { get; init; } = new();
}

public record TaskAttributes
{
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string TaskType { get; init; } = string.Empty;
    public string Frequency { get; init; } = string.Empty;
    public Guid? DimensionId { get; init; }
    public string? DimensionCode { get; init; }
    public Guid? MilestoneId { get; init; }
    public string? LinkedMetricCode { get; init; }
    public DateOnly? ScheduledDate { get; init; }
    public TimeOnly? ScheduledTime { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public bool IsCompleted { get; init; }
    public DateTime? CompletedAt { get; init; }
    public bool IsActive { get; init; }
    public string[]? Tags { get; init; }
    public int CurrentStreak { get; init; }
    public int LongestStreak { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record TaskDetailResponse
{
    public TaskItemResponse Data { get; init; } = new();
}

public record PaginationMeta
{
    public int Page { get; init; }
    public int PerPage { get; init; }
    public int Total { get; init; }
    public int TotalPages { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public record CreateTaskRequest
{
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string TaskType { get; init; } = "one_off";
    public string Frequency { get; init; } = "ad_hoc";
    public Guid? DimensionId { get; init; }
    public Guid? MilestoneId { get; init; }
    public string? LinkedMetricCode { get; init; }
    public DateOnly? ScheduledDate { get; init; }
    public TimeOnly? ScheduledTime { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public string[]? Tags { get; init; }
}

public record UpdateTaskRequest
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? Frequency { get; init; }
    public DateOnly? ScheduledDate { get; init; }
    public TimeOnly? ScheduledTime { get; init; }
    public DateOnly? EndDate { get; init; }
    public bool? IsActive { get; init; }
    public string[]? Tags { get; init; }
}

public record CompleteTaskRequest
{
    public DateTime? CompletedAt { get; init; }
    public decimal? MetricValue { get; init; }
}

public record TaskCompletionResponse
{
    public TaskCompletionData Data { get; init; } = new();
}

public record TaskCompletionData
{
    public Guid Id { get; init; }
    public string Type { get; init; } = "taskCompletion";
    public TaskCompletionAttributes Attributes { get; init; } = new();
}

public record TaskCompletionAttributes
{
    public Guid TaskId { get; init; }
    public DateTime CompletedAt { get; init; }
    public int CurrentStreak { get; init; }
    public int LongestStreak { get; init; }
    public bool MetricRecorded { get; init; }
}
