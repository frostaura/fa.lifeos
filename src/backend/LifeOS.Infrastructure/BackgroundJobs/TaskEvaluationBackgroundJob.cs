using LifeOS.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace LifeOS.Infrastructure.BackgroundJobs;

/// <summary>
/// Background job for evaluating tasks based on metric conditions
/// v3.0: Task Auto-Evaluation feature - Runs hourly to evaluate tasks and create auto-completions
/// </summary>
public class TaskEvaluationBackgroundJob
{
    private readonly ITaskEvaluationService _evaluationService;
    private readonly ILogger<TaskEvaluationBackgroundJob> _logger;

    public TaskEvaluationBackgroundJob(
        ITaskEvaluationService evaluationService,
        ILogger<TaskEvaluationBackgroundJob> logger)
    {
        _evaluationService = evaluationService;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting task evaluation background job");

        try
        {
            // Evaluate all tasks for today (UTC)
            var evaluationDate = DateTime.UtcNow;
            await _evaluationService.EvaluateAllTasksAsync(evaluationDate, cancellationToken);
            
            _logger.LogInformation("Task evaluation background job completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Task evaluation background job failed");
            throw;
        }
    }
}
