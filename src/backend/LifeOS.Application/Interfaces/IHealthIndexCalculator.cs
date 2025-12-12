namespace LifeOS.Application.Interfaces;

/// <summary>
/// Calculates the Health Index score (0-100) by aggregating health metrics
/// with per-metric normalization based on target directions.
/// </summary>
public interface IHealthIndexCalculator
{
    /// <summary>
    /// Calculates the Health Index for a user at a specific point in time.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="asOfDate">Optional date for historical calculation (defaults to now)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health Index result with score and component breakdown</returns>
    Task<HealthIndexResult> CalculateAsync(
        Guid userId, 
        DateTime? asOfDate = null, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Saves a Health Index calculation result as a snapshot for historical tracking.
    /// </summary>
    /// <param name="result">The calculated health index result</param>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The persisted snapshot entity</returns>
    Task<Domain.Entities.HealthIndexSnapshot> SaveSnapshotAsync(
        HealthIndexResult result, 
        Guid userId, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of Health Index calculation containing the overall score and per-metric components.
/// </summary>
public class HealthIndexResult
{
    /// <summary>
    /// Overall Health Index score (0-100)
    /// </summary>
    public decimal Score { get; set; }
    
    /// <summary>
    /// Individual metric scores that contribute to the overall score
    /// </summary>
    public List<HealthMetricScore> Components { get; set; } = new();
    
    /// <summary>
    /// When the calculation was performed
    /// </summary>
    public DateTime CalculatedAt { get; set; }
}

/// <summary>
/// Individual metric score component of the Health Index.
/// </summary>
public class HealthMetricScore
{
    /// <summary>
    /// Metric code (e.g., "resting_hr", "body_fat_pct")
    /// </summary>
    public string MetricCode { get; set; } = string.Empty;
    
    /// <summary>
    /// Individual metric score (0-100)
    /// </summary>
    public decimal Score { get; set; }
    
    /// <summary>
    /// Weight of this metric in overall calculation (0-1)
    /// </summary>
    public decimal Weight { get; set; }
    
    /// <summary>
    /// The actual recorded value
    /// </summary>
    public decimal? ActualValue { get; set; }
    
    /// <summary>
    /// The target value for this metric
    /// </summary>
    public decimal? TargetValue { get; set; }
}
