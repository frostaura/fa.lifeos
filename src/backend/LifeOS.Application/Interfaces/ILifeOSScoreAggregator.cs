namespace LifeOS.Application.Interfaces;

/// <summary>
/// Aggregates all v3.0 scoring systems (Health Index, Adherence, Wealth Health, Longevity)
/// into the comprehensive LifeOS Score (0-100).
/// </summary>
public interface ILifeOSScoreAggregator
{
    /// <summary>
    /// Calculates the comprehensive LifeOS Score by aggregating all component scores.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="asOfDate">Optional date for historical calculation (defaults to now)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>LifeOS Score result with all component scores</returns>
    Task<LifeOsScoreResult> CalculateAsync(
        Guid userId, 
        DateTime? asOfDate = null, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Saves a LifeOS Score calculation result as a snapshot for historical tracking.
    /// </summary>
    /// <param name="result">The calculated LifeOS Score result</param>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The persisted snapshot entity</returns>
    Task<Domain.Entities.LifeOsScoreSnapshot> SaveSnapshotAsync(
        LifeOsScoreResult result, 
        Guid userId, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of comprehensive LifeOS Score calculation containing all component scores.
/// </summary>
public class LifeOsScoreResult
{
    /// <summary>
    /// Overall LifeOS Score (0-100)
    /// Formula: wH × HealthIndex + wA × AdherenceIndex + wW × WealthHealthScore
    /// </summary>
    public decimal LifeScore { get; set; }
    
    /// <summary>
    /// Health Index component (0-100)
    /// </summary>
    public decimal HealthIndex { get; set; }
    
    /// <summary>
    /// Behavioral Adherence Index component (0-100)
    /// </summary>
    public decimal AdherenceIndex { get; set; }
    
    /// <summary>
    /// Wealth Health Score component (0-100)
    /// </summary>
    public decimal WealthHealthScore { get; set; }
    
    /// <summary>
    /// Longevity years added (placeholder 0 for now - Epic 3)
    /// </summary>
    public decimal LongevityYearsAdded { get; set; }
    
    /// <summary>
    /// Per-dimension score breakdown (future enhancement)
    /// </summary>
    public List<DimensionScoreEntry> DimensionScores { get; set; } = new();
    
    /// <summary>
    /// When the calculation was performed
    /// </summary>
    public DateTime CalculatedAt { get; set; }
}

/// <summary>
/// Individual dimension score entry for detailed breakdown.
/// </summary>
public class DimensionScoreEntry
{
    /// <summary>
    /// Dimension code (e.g., "health", "relationships", "career")
    /// </summary>
    public string DimensionCode { get; set; } = string.Empty;
    
    /// <summary>
    /// Dimension score (0-100)
    /// </summary>
    public decimal Score { get; set; }
    
    /// <summary>
    /// Weight of this dimension in overall calculation
    /// </summary>
    public decimal Weight { get; set; }
}
