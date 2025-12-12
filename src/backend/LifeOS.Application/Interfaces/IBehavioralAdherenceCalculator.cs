namespace LifeOS.Application.Interfaces;

/// <summary>
/// Calculates the Behavioral Adherence Index (0-100) by combining task completion
/// rates with streak penalty factors.
/// </summary>
public interface IBehavioralAdherenceCalculator
{
    /// <summary>
    /// Calculates the Behavioral Adherence Index for a user over a time window.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="asOfDate">Optional date for historical calculation (defaults to now)</param>
    /// <param name="lookbackDays">Number of days to look back (default: 7)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Adherence result with score and calculation components</returns>
    Task<AdherenceResult> CalculateAsync(
        Guid userId, 
        DateTime? asOfDate = null, 
        int lookbackDays = 7, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Saves an Adherence calculation result as a snapshot for historical tracking.
    /// </summary>
    /// <param name="result">The calculated adherence result</param>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The persisted snapshot entity</returns>
    Task<Domain.Entities.AdherenceSnapshot> SaveSnapshotAsync(
        AdherenceResult result, 
        Guid userId, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of Behavioral Adherence Index calculation.
/// </summary>
public class AdherenceResult
{
    /// <summary>
    /// Overall Adherence Index score (0-100)
    /// Formula: rawAdherence × 100 × penaltyFactor
    /// </summary>
    public decimal Score { get; set; }
    
    /// <summary>
    /// Raw completion rate (completed / scheduled)
    /// </summary>
    public decimal RawAdherence { get; set; }
    
    /// <summary>
    /// Penalty factor from streak misses (clamped 0.5-1.0)
    /// Formula: clamp(1 - avgPenalty, 0.5, 1.0)
    /// </summary>
    public decimal PenaltyFactor { get; set; }
    
    /// <summary>
    /// Time window in days
    /// </summary>
    public int TimeWindowDays { get; set; }
    
    /// <summary>
    /// Number of scheduled task instances in window
    /// </summary>
    public int TasksScheduled { get; set; }
    
    /// <summary>
    /// Number of completed task instances in window
    /// </summary>
    public int TasksCompleted { get; set; }
    
    /// <summary>
    /// When the calculation was performed
    /// </summary>
    public DateTime CalculatedAt { get; set; }
}
