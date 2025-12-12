namespace LifeOS.Application.Interfaces;

/// <summary>
/// Service for evaluating tasks based on metric conditions and creating auto-completions
/// v3.0: Task Auto-Evaluation feature
/// </summary>
public interface ITaskEvaluationService
{
    /// <summary>
    /// Evaluates a single task against its metric linkage and creates completion if condition met
    /// </summary>
    Task EvaluateTaskAsync(Guid taskId, DateTime evaluationDate, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Evaluates all tasks with metric linkage for the given date
    /// </summary>
    Task EvaluateAllTasksAsync(DateTime evaluationDate, CancellationToken cancellationToken = default);
}
