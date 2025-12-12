namespace LifeOS.Application.Interfaces;

/// <summary>
/// v3.0: Primary Stats Calculator
/// Calculates primary stats (strength, wisdom, charisma, composure, energy, influence, vitality)
/// from dimension scores using weighted formulas.
/// </summary>
public interface IPrimaryStatsCalculator
{
    /// <summary>
    /// Calculates primary stats for a user based on dimension scores.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="asOfDate">Optional date for historical calculation (defaults to now)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Primary stats calculation result with all stat values and dimension scores</returns>
    Task<PrimaryStatsResult> CalculateAsync(
        Guid userId, 
        DateTime? asOfDate = null, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Saves a primary stats calculation result as a snapshot for historical tracking.
    /// </summary>
    /// <param name="result">The calculated primary stats result</param>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The persisted snapshot entity</returns>
    Task<Domain.Entities.PrimaryStatRecord> SaveSnapshotAsync(
        PrimaryStatsResult result, 
        Guid userId, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of primary stats calculation containing all stat values and dimension scores.
/// </summary>
public class PrimaryStatsResult
{
    /// <summary>
    /// Primary stat values (stat code → 0-100 score)
    /// e.g., { "strength": 72.5, "wisdom": 88.0, ... }
    /// </summary>
    public Dictionary<string, decimal> Values { get; set; } = new();
    
    /// <summary>
    /// Dimension scores used in calculation (dimension code → 0-100 score)
    /// e.g., { "health": 85.0, "relationships": 70.0, ... }
    /// </summary>
    public Dictionary<string, decimal> DimensionScores { get; set; } = new();
    
    /// <summary>
    /// When the calculation was performed
    /// </summary>
    public DateTime CalculatedAt { get; set; }
    
    /// <summary>
    /// Detailed breakdown of calculations (for debugging/transparency)
    /// </summary>
    public Dictionary<string, object> CalculationDetails { get; set; } = new();
}
