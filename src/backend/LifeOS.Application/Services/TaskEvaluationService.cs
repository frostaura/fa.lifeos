using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.Interfaces;
using LifeOS.Domain.Entities;
using LifeOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LifeOS.Application.Services;

/// <summary>
/// Service for evaluating tasks based on metric conditions and creating auto-completions
/// v3.0: Task Auto-Evaluation feature - Background job evaluates tasks and creates completions when conditions met
/// </summary>
public class TaskEvaluationService : ITaskEvaluationService
{
    private readonly ILifeOSDbContext _context;
    private readonly IMetricAggregationService _metricAggregationService;
    private readonly ILogger<TaskEvaluationService> _logger;

    public TaskEvaluationService(
        ILifeOSDbContext context,
        IMetricAggregationService metricAggregationService,
        ILogger<TaskEvaluationService> logger)
    {
        _context = context;
        _metricAggregationService = metricAggregationService;
        _logger = logger;
    }

    public async Task EvaluateAllTasksAsync(DateTime evaluationDate, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting task evaluation for date {Date}", evaluationDate.Date);

        try
        {
            // Get all active tasks with metric linkage
            var tasksWithMetrics = await _context.Tasks
                .Where(t => t.IsActive && t.MetricCode != null && t.TargetValue != null && t.TargetComparison != null)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Found {Count} tasks with metric linkage to evaluate", tasksWithMetrics.Count);

            var evaluatedCount = 0;
            var completedCount = 0;
            var skippedCount = 0;
            var errorCount = 0;

            foreach (var task in tasksWithMetrics)
            {
                try
                {
                    var wasCompleted = await EvaluateTaskInternalAsync(task, evaluationDate, cancellationToken);
                    evaluatedCount++;
                    if (wasCompleted)
                    {
                        completedCount++;
                    }
                    else
                    {
                        skippedCount++;
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    _logger.LogError(ex, "Error evaluating task {TaskId} ({TaskTitle})", task.Id, task.Title);
                }
            }

            _logger.LogInformation(
                "Task evaluation completed. Evaluated: {Evaluated}, Completed: {Completed}, Skipped: {Skipped}, Errors: {Errors}",
                evaluatedCount, completedCount, skippedCount, errorCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Task evaluation batch failed");
            throw;
        }
    }

    public async Task EvaluateTaskAsync(Guid taskId, DateTime evaluationDate, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Evaluating task {TaskId} for date {Date}", taskId, evaluationDate.Date);

        // Load task with metric linkage
        var task = await _context.Tasks
            .Include(t => t.Dimension)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.IsActive && t.MetricCode != null, cancellationToken);

        if (task == null)
        {
            _logger.LogWarning("Task {TaskId} not found or has no metric linkage", taskId);
            return;
        }

        await EvaluateTaskInternalAsync(task, evaluationDate, cancellationToken);
    }

    private async Task<bool> EvaluateTaskInternalAsync(LifeTask task, DateTime evaluationDate, CancellationToken cancellationToken)
    {
        // Validate task configuration
        if (string.IsNullOrEmpty(task.MetricCode) || !task.TargetValue.HasValue || !task.TargetComparison.HasValue)
        {
            _logger.LogWarning("Task {TaskId} ({TaskTitle}) has invalid metric configuration", task.Id, task.Title);
            return false;
        }

        // Check for duplicate completion
        var existingCompletion = await _context.TaskCompletions
            .AnyAsync(tc => tc.TaskId == task.Id && tc.CompletedAt.Date == evaluationDate.Date, cancellationToken);

        if (existingCompletion)
        {
            _logger.LogDebug("Task {TaskId} ({TaskTitle}) already has completion for {Date}", 
                task.Id, task.Title, evaluationDate.Date);
            return false;
        }

        // Determine evaluation window based on task frequency
        var (startTime, endTime) = GetEvaluationWindow(task, evaluationDate);

        // Aggregate metric value
        var aggregatedValue = await _metricAggregationService.AggregateMetricAsync(
            task.MetricCode,
            task.UserId,
            startTime,
            endTime,
            cancellationToken);

        if (!aggregatedValue.HasValue)
        {
            _logger.LogDebug("Task {TaskId} ({TaskTitle}): No metric data found for {MetricCode} in window {Start} to {End}",
                task.Id, task.Title, task.MetricCode, startTime, endTime);
            return false;
        }

        // Evaluate condition
        var conditionMet = EvaluateCondition(aggregatedValue.Value, task.TargetValue.Value, task.TargetComparison.Value);

        if (!conditionMet)
        {
            _logger.LogDebug("Task {TaskId} ({TaskTitle}): Condition not met. Actual: {Actual}, Target: {Target} {Comparison}",
                task.Id, task.Title, aggregatedValue.Value, task.TargetValue.Value, task.TargetComparison.Value);
            return false;
        }

        // Create auto-completion
        var completion = new TaskCompletion
        {
            TaskId = task.Id,
            UserId = task.UserId,
            CompletedAt = evaluationDate,
            CompletionType = CompletionType.AutoMetric,
            ValueNumber = aggregatedValue.Value,
            Notes = $"Auto-evaluated: {task.MetricCode} {GetComparisonSymbol(task.TargetComparison.Value)} {task.TargetValue.Value}"
        };

        _context.TaskCompletions.Add(completion);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Task {TaskId} ({TaskTitle}) auto-completed. Metric value: {Value}, Target: {Target} {Comparison}",
            task.Id, task.Title, aggregatedValue.Value, task.TargetValue.Value, task.TargetComparison.Value);

        return true;
    }

    private bool EvaluateCondition(decimal actualValue, decimal targetValue, TaskTargetComparison comparison)
    {
        return comparison switch
        {
            TaskTargetComparison.GreaterThanOrEqual => actualValue >= targetValue,
            TaskTargetComparison.LessThanOrEqual => actualValue <= targetValue,
            TaskTargetComparison.Equal => Math.Abs(actualValue - targetValue) < 0.0001m,
            _ => false
        };
    }

    private (DateTime start, DateTime end) GetEvaluationWindow(LifeTask task, DateTime date)
    {
        // Use the task's frequency to determine the evaluation window
        return task.Frequency switch
        {
            Frequency.Daily => (date.Date, date.Date.AddDays(1).AddTicks(-1)),
            Frequency.Weekly => GetWeeklyWindow(date),
            Frequency.Monthly => GetMonthlyWindow(date),
            Frequency.Quarterly => GetQuarterlyWindow(date),
            Frequency.Yearly => GetYearlyWindow(date),
            _ => (date.Date, date.Date.AddDays(1).AddTicks(-1)) // Default to daily
        };
    }

    private (DateTime start, DateTime end) GetWeeklyWindow(DateTime date)
    {
        // Get start of week (Monday)
        var daysFromMonday = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        var startOfWeek = date.Date.AddDays(-daysFromMonday);
        return (startOfWeek, startOfWeek.AddDays(7).AddTicks(-1));
    }

    private (DateTime start, DateTime end) GetMonthlyWindow(DateTime date)
    {
        var startOfMonth = new DateTime(date.Year, date.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddTicks(-1);
        return (startOfMonth, endOfMonth);
    }

    private (DateTime start, DateTime end) GetQuarterlyWindow(DateTime date)
    {
        var currentQuarter = (date.Month - 1) / 3;
        var startOfQuarter = new DateTime(date.Year, currentQuarter * 3 + 1, 1);
        var endOfQuarter = startOfQuarter.AddMonths(3).AddTicks(-1);
        return (startOfQuarter, endOfQuarter);
    }

    private (DateTime start, DateTime end) GetYearlyWindow(DateTime date)
    {
        var startOfYear = new DateTime(date.Year, 1, 1);
        var endOfYear = startOfYear.AddYears(1).AddTicks(-1);
        return (startOfYear, endOfYear);
    }

    private string GetComparisonSymbol(TaskTargetComparison comparison)
    {
        return comparison switch
        {
            TaskTargetComparison.GreaterThanOrEqual => ">=",
            TaskTargetComparison.LessThanOrEqual => "<=",
            TaskTargetComparison.Equal => "=",
            _ => "?"
        };
    }
}
