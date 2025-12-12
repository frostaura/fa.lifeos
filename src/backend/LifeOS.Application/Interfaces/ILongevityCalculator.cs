namespace LifeOS.Application.Interfaces;

/// <summary>
/// v3.0: Longevity Calculation Engine
/// Uses multiplicative risk reduction formula to calculate years added to life expectancy.
/// </summary>
public interface ILongevityCalculator
{
    /// <summary>
    /// Calculates longevity estimate using multiplicative risk reduction formula.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="asOfDate">Optional date for historical calculation (defaults to now)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Longevity calculation result</returns>
    Task<LongevityResult> CalculateAsync(Guid userId, DateTime? asOfDate = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Saves a longevity calculation result as a snapshot for historical tracking.
    /// </summary>
    /// <param name="result">The calculated longevity result</param>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The persisted snapshot entity</returns>
    Task<Domain.Entities.LongevitySnapshot> SaveSnapshotAsync(LongevityResult result, Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of longevity calculation using multiplicative risk reduction formula.
/// </summary>
public class LongevityResult
{
    /// <summary>
    /// Baseline life expectancy in years (from user profile or actuarial table)
    /// </summary>
    public decimal BaselineLifeExpectancyYears { get; set; }
    
    /// <summary>
    /// Adjusted life expectancy after applying risk reductions
    /// </summary>
    public decimal AdjustedLifeExpectancyYears { get; set; }
    
    /// <summary>
    /// Total years added through behavioral improvements (capped at maxYearsCap)
    /// </summary>
    public decimal TotalYearsAdded { get; set; }
    
    /// <summary>
    /// Combined risk factor using multiplicative formula: Π(1 - r_i)
    /// </summary>
    public decimal RiskFactorCombined { get; set; }
    
    /// <summary>
    /// Per-model breakdown of risk reductions and years added
    /// </summary>
    public List<LongevityComponent> Breakdown { get; set; } = new();
    
    /// <summary>
    /// Confidence level: low, medium, high (based on data availability)
    /// </summary>
    public string Confidence { get; set; } = "medium";
    
    /// <summary>
    /// When the calculation was performed
    /// </summary>
    public DateTime CalculatedAt { get; set; }
}

/// <summary>
/// Individual model contribution to longevity calculation.
/// </summary>
public class LongevityComponent
{
    /// <summary>
    /// Model code (e.g., "steps_10k", "body_fat_optimal")
    /// </summary>
    public string ModelCode { get; set; } = string.Empty;
    
    /// <summary>
    /// Risk reduction factor for this model (0.0 to 0.9)
    /// </summary>
    public decimal RiskReduction { get; set; }
    
    /// <summary>
    /// Estimated years added by this specific model (informational, not summed)
    /// </summary>
    public decimal EstimatedYearsAdded { get; set; }
}
