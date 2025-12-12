using System.ComponentModel;
using LifeOS.Application.Commands.Tasks;
using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Mcp;
using LifeOS.Application.Queries.Tasks;
using LifeOS.Application.Services;
using LifeOS.Application.Services.Mcp;
using LifeOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;

namespace LifeOS.Api.Mcp.Tools;

/// <summary>
/// MCP Tools for task management operations.
/// </summary>
[McpServerToolType]
public class TaskTools
{
    private readonly ILifeOSDbContext _dbContext;
    private readonly IStreakService _streakService;
    private readonly IMediator _mediator;
    private readonly IMcpApiKeyValidator _apiKeyValidator;

    public TaskTools(
        ILifeOSDbContext dbContext,
        IStreakService streakService,
        IMediator mediator,
        IMcpApiKeyValidator apiKeyValidator)
    {
        _dbContext = dbContext;
        _streakService = streakService;
        _mediator = mediator;
        _apiKeyValidator = apiKeyValidator;
    }

    /// <summary>
    /// List tasks with filtering options.
    /// </summary>
    [McpServerTool(Name = "listTasks"), Description("List tasks with optional filtering by dimension, type, and completion status. Example response: { Success: true, Data: { Tasks: [ { Id: <guid>, Title: \"Meditate\", DimensionCode: \"mind\", Frequency: \"daily\", IsCompleted: false, CurrentStreak: 14 } ], TotalCount: 1 }, Error: null }")]
    public async Task<McpToolResponse<ListTasksResponse>> ListTasks(
        [Description("API key for authentication")] string apiKey,
        [Description("Request parameters")] ListTasksRequest request,
        CancellationToken cancellationToken = default)
    {
        var authResult = await _apiKeyValidator.ValidateAndGetUserIdAsync(apiKey, cancellationToken);
        if (!authResult.IsValid)
            return McpToolResponse<ListTasksResponse>.Fail(authResult.Error!);

        var query = _dbContext.Tasks
            .Where(t => t.UserId == authResult.UserId)
            .Include(t => t.Dimension)
            .Include(t => t.Streaks)
            .Include(t => t.TaskCompletions)
            .AsQueryable();

        if (!string.IsNullOrEmpty(request.DimensionCode))
            query = query.Where(t => t.Dimension != null && t.Dimension.Code == request.DimensionCode);

        if (!string.IsNullOrEmpty(request.TaskType) && Enum.TryParse<TaskType>(request.TaskType, true, out var taskType))
            query = query.Where(t => t.TaskType == taskType);

        if (request.OnlyActive)
            query = query.Where(t => t.IsActive);

        if (!request.IncludeCompleted)
            query = query.Where(t => !t.IsCompleted);

        var tasks = await query
            .OrderBy(t => t.Dimension != null ? t.Dimension.Code : "zzz")
            .ThenBy(t => t.Title)
            .Take(100)
            .ToListAsync(cancellationToken);

        var response = new ListTasksResponse
        {
            Tasks = tasks.Select(t =>
            {
                var streak = t.Streaks.FirstOrDefault(s => s.IsActive);
                var lastCompletion = t.TaskCompletions.OrderByDescending(tc => tc.CompletedAt).FirstOrDefault();
                return new TaskSummary
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description ?? string.Empty,
                    DimensionCode = t.Dimension?.Code ?? string.Empty,
                    TaskType = t.TaskType.ToString().ToLower(),
                    Frequency = t.Frequency.ToString().ToLower(),
                    IsActive = t.IsActive,
                    IsCompleted = t.IsCompleted,
                    CurrentStreak = streak?.CurrentStreakLength ?? 0,
                    LongestStreak = streak?.LongestStreakLength ?? 0,
                    LastCompletedAt = lastCompletion?.CompletedAt,
                    LinkedMetricCode = t.LinkedMetricCode ?? string.Empty,
                    TargetValue = t.TargetValue,
                    Tags = t.Tags?.ToList() ?? new()
                };
            }).ToList(),
            TotalCount = tasks.Count
        };

        return McpToolResponse<ListTasksResponse>.Ok(response);
    }

    /// <summary>
    /// Get a single task by ID.
    /// </summary>
    [McpServerTool(Name = "getTask"), Description("Get detailed information about a specific task. Example response: { Success: true, Data: { Task: { Id: <guid>, Title: \"Meditate\", Frequency: \"daily\", IsActive: true, IsCompleted: false, Streak: { CurrentLength: 14, LongestLength: 30 } } }, Error: null }")]
    public async Task<McpToolResponse<GetTaskResponse>> GetTask(
        [Description("API key for authentication")] string apiKey,
        [Description("Request parameters")] GetTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        var authResult = await _apiKeyValidator.ValidateAndGetUserIdAsync(apiKey, cancellationToken);
        if (!authResult.IsValid)
            return McpToolResponse<GetTaskResponse>.Fail(authResult.Error!);

        var task = await _dbContext.Tasks
            .Include(t => t.Dimension)
            .Include(t => t.Streaks)
            .Include(t => t.TaskCompletions)
            .FirstOrDefaultAsync(t => t.Id == request.TaskId && t.UserId == authResult.UserId, cancellationToken);

        if (task == null)
            return McpToolResponse<GetTaskResponse>.Fail("Task not found.");

        var streak = task.Streaks.FirstOrDefault(s => s.IsActive);
        var response = new GetTaskResponse
        {
            Task = new TaskDetail
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description ?? string.Empty,
                TaskType = task.TaskType.ToString().ToLower(),
                Frequency = task.Frequency.ToString().ToLower(),
                DimensionId = task.DimensionId,
                DimensionCode = task.Dimension?.Code ?? string.Empty,
                MilestoneId = task.MilestoneId,
                LinkedMetricCode = task.LinkedMetricCode ?? string.Empty,
                TargetValue = task.TargetValue,
                TargetComparison = task.TargetComparison?.ToString().ToLower() ?? string.Empty,
                ScheduledDate = task.ScheduledDate?.ToDateTime(TimeOnly.MinValue),
                ScheduledTime = task.ScheduledTime?.ToTimeSpan(),
                StartDate = task.StartDate.ToDateTime(TimeOnly.MinValue),
                EndDate = task.EndDate?.ToDateTime(TimeOnly.MinValue),
                IsActive = task.IsActive,
                IsCompleted = task.IsCompleted,
                Tags = task.Tags?.ToList() ?? new(),
                Streak = new StreakInfo
                {
                    CurrentLength = streak?.CurrentStreakLength ?? 0,
                    LongestLength = streak?.LongestStreakLength ?? 0,
                    RiskPenalty = streak?.RiskPenaltyScore ?? 0,
                    ConsecutiveMisses = streak?.ConsecutiveMisses ?? 0
                },
                CreatedAt = task.CreatedAt,
                UpdatedAt = task.UpdatedAt ?? task.CreatedAt
            }
        };

        return McpToolResponse<GetTaskResponse>.Ok(response);
    }

    /// <summary>
    /// Create a new task.
    /// </summary>
    [McpServerTool(Name = "createTask"), Description("Create a new task, habit, or todo. Example response: { Success: true, Data: { TaskId: <guid>, Task: { Id: <guid>, Title: \"Meditate\", Frequency: \"daily\", IsActive: true, IsCompleted: false } }, Error: null }")]
    public async Task<McpToolResponse<CreateTaskResponse>> CreateTask(
        [Description("API key for authentication")] string apiKey,
        [Description("Request parameters")] Application.DTOs.Mcp.CreateTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        var authResult = await _apiKeyValidator.ValidateAndGetUserIdAsync(apiKey, cancellationToken);
        if (!authResult.IsValid)
            return McpToolResponse<CreateTaskResponse>.Fail(authResult.Error!);

        if (string.IsNullOrEmpty(request.Title))
            return McpToolResponse<CreateTaskResponse>.Fail("Title is required.");

        if (string.IsNullOrEmpty(request.TaskType))
            return McpToolResponse<CreateTaskResponse>.Fail("TaskType is required.");

        var result = await _mediator.Send(new CreateTaskCommand(
            authResult.UserId,
            request.Title,
            request.Description,
            request.TaskType,
            request.Frequency,
            request.DimensionId,
            request.MilestoneId,
            request.LinkedMetricCode,
            request.ScheduledDate.HasValue ? DateOnly.FromDateTime(request.ScheduledDate.Value) : null,
            null,
            null,
            null,
            request.Tags?.ToArray()), cancellationToken);

        return McpToolResponse<CreateTaskResponse>.Ok(new CreateTaskResponse
        {
            TaskId = result.Data.Id,
            Task = new TaskDetail
            {
                Id = result.Data.Id,
                Title = result.Data.Attributes.Title,
                Description = result.Data.Attributes.Description ?? string.Empty,
                TaskType = result.Data.Attributes.TaskType,
                Frequency = result.Data.Attributes.Frequency,
                DimensionCode = result.Data.Attributes.DimensionCode ?? string.Empty,
                IsActive = result.Data.Attributes.IsActive,
                IsCompleted = result.Data.Attributes.IsCompleted,
                Tags = result.Data.Attributes.Tags?.ToList() ?? new(),
                Streak = new StreakInfo(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        });
    }

    /// <summary>
    /// Update an existing task.
    /// </summary>
    [McpServerTool(Name = "updateTask"), Description("Update an existing task's properties. Example response: { Success: true, Data: { Success: true, Task: { Id: <guid>, Title: \"Meditate\", IsActive: true, IsCompleted: false } }, Error: null }")]
    public async Task<McpToolResponse<UpdateTaskResponse>> UpdateTask(
        [Description("API key for authentication")] string apiKey,
        [Description("Request parameters")] Application.DTOs.Mcp.UpdateTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        var authResult = await _apiKeyValidator.ValidateAndGetUserIdAsync(apiKey, cancellationToken);
        if (!authResult.IsValid)
            return McpToolResponse<UpdateTaskResponse>.Fail(authResult.Error!);

        var success = await _mediator.Send(new UpdateTaskCommand(
            authResult.UserId,
            request.TaskId,
            string.IsNullOrEmpty(request.Title) ? null : request.Title,
            string.IsNullOrEmpty(request.Description) ? null : request.Description,
            string.IsNullOrEmpty(request.Frequency) ? null : request.Frequency,
            request.ScheduledDate.HasValue ? DateOnly.FromDateTime(request.ScheduledDate.Value) : null,
            null,
            request.EndDate.HasValue ? DateOnly.FromDateTime(request.EndDate.Value) : null,
            request.IsActive,
            request.Tags?.ToArray()), cancellationToken);

        if (!success)
            return McpToolResponse<UpdateTaskResponse>.Fail("Task not found.");

        var taskResult = await _mediator.Send(new GetTaskByIdQuery(authResult.UserId, request.TaskId), cancellationToken);
        return McpToolResponse<UpdateTaskResponse>.Ok(new UpdateTaskResponse
        {
            Success = true,
            Task = new TaskDetail
            {
                Id = taskResult!.Data.Id,
                Title = taskResult.Data.Attributes.Title,
                Description = taskResult.Data.Attributes.Description ?? string.Empty,
                TaskType = taskResult.Data.Attributes.TaskType,
                Frequency = taskResult.Data.Attributes.Frequency,
                IsActive = taskResult.Data.Attributes.IsActive,
                IsCompleted = taskResult.Data.Attributes.IsCompleted,
                Tags = taskResult.Data.Attributes.Tags?.ToList() ?? new(),
                Streak = new StreakInfo
                {
                    CurrentLength = taskResult.Data.Attributes.CurrentStreak,
                    LongestLength = taskResult.Data.Attributes.LongestStreak,
                    RiskPenalty = 0,
                    ConsecutiveMisses = 0
                },
                CreatedAt = taskResult.Data.Attributes.CreatedAt,
                UpdatedAt = DateTime.UtcNow
            }
        });
    }

    /// <summary>
    /// Delete a task.
    /// </summary>
    [McpServerTool(Name = "deleteTask"), Description("Delete a task permanently. Example response: { Success: true, Data: { Success: true, DeletedTaskId: <guid> }, Error: null }")]
    public async Task<McpToolResponse<DeleteTaskResponse>> DeleteTask(
        [Description("API key for authentication")] string apiKey,
        [Description("Request parameters")] DeleteTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        var authResult = await _apiKeyValidator.ValidateAndGetUserIdAsync(apiKey, cancellationToken);
        if (!authResult.IsValid)
            return McpToolResponse<DeleteTaskResponse>.Fail(authResult.Error!);

        var success = await _mediator.Send(new DeleteTaskCommand(authResult.UserId, request.TaskId), cancellationToken);
        if (!success)
            return McpToolResponse<DeleteTaskResponse>.Fail("Task not found.");

        return McpToolResponse<DeleteTaskResponse>.Ok(new DeleteTaskResponse
        {
            Success = true,
            DeletedTaskId = request.TaskId
        });
    }

    /// <summary>
    /// Complete a task and update its streak.
    /// </summary>
    [McpServerTool(Name = "completeTask"), Description("Mark a task as complete and update its streak. Example response: { Success: true, Data: { Success: true, CompletionId: <guid>, UpdatedStreak: { CurrentLength: 15, LongestLength: 30 }, MetricRecorded: true }, Error: null }")]
    public async Task<McpToolResponse<Application.DTOs.Mcp.CompleteTaskResponse>> CompleteTask(
        [Description("API key for authentication")] string apiKey,
        [Description("Request parameters")] Application.DTOs.Mcp.CompleteTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        var authResult = await _apiKeyValidator.ValidateAndGetUserIdAsync(apiKey, cancellationToken);
        if (!authResult.IsValid)
            return McpToolResponse<Application.DTOs.Mcp.CompleteTaskResponse>.Fail(authResult.Error!);

        var result = await _mediator.Send(new CompleteTaskCommand(
            authResult.UserId,
            request.TaskId,
            DateTime.UtcNow,
            request.MetricValue), cancellationToken);

        if (result == null)
            return McpToolResponse<Application.DTOs.Mcp.CompleteTaskResponse>.Fail("Task not found.");

        return McpToolResponse<Application.DTOs.Mcp.CompleteTaskResponse>.Ok(new Application.DTOs.Mcp.CompleteTaskResponse
        {
            Success = true,
            CompletionId = result.Data.Id,
            UpdatedStreak = new StreakInfo
            {
                CurrentLength = result.Data.Attributes.CurrentStreak,
                LongestLength = result.Data.Attributes.LongestStreak,
                RiskPenalty = 0,
                ConsecutiveMisses = 0
            },
            MetricRecorded = result.Data.Attributes.MetricRecorded,
            MetricRecordId = null
        });
    }
}
