namespace LifeOS.Application.Interfaces;

/// <summary>
/// Calculates the Wealth Health score (0-100) by aggregating 5 finance components:
/// savings rate, debt-to-income, emergency fund, diversification, and net worth growth.
/// </summary>
public interface IWealthHealthCalculator
{
    /// <summary>
    /// Calculates the Wealth Health score for a user at a specific point in time.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="asOfDate">Optional date for historical calculation (defaults to now)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Wealth Health result with score and component breakdown</returns>
    Task<WealthHealthCalculation> CalculateAsync(
        Guid userId, 
        DateTime? asOfDate = null, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Saves a Wealth Health calculation result as a snapshot for historical tracking.
    /// </summary>
    /// <param name="result">The calculated wealth health result</param>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The persisted snapshot entity</returns>
    Task<Domain.Entities.WealthHealthSnapshot> SaveSnapshotAsync(
        WealthHealthCalculation result, 
        Guid userId, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of Wealth Health calculation containing the overall score and per-component breakdown.
/// </summary>
public class WealthHealthCalculation
{
    /// <summary>
    /// Overall Wealth Health score (0-100)
    /// </summary>
    public decimal Score { get; set; }
    
    /// <summary>
    /// Individual component scores that contribute to the overall score
    /// </summary>
    public List<WealthComponent> Components { get; set; } = new();
    
    /// <summary>
    /// When the calculation was performed
    /// </summary>
    public DateTime CalculatedAt { get; set; }
}

/// <summary>
/// Individual component score of the Wealth Health calculation.
/// </summary>
public class WealthComponent
{
    /// <summary>
    /// Component code (e.g., "savings_rate", "debt_to_income", "emergency_fund", "diversification", "net_worth_growth")
    /// </summary>
    public string ComponentCode { get; set; } = string.Empty;
    
    /// <summary>
    /// Individual component score (0-100)
    /// </summary>
    public decimal Score { get; set; }
    
    /// <summary>
    /// Weight of this component in overall calculation
    /// </summary>
    public decimal Weight { get; set; }
    
    /// <summary>
    /// The actual calculated value (e.g., 0.25 for 25% savings rate)
    /// </summary>
    public decimal? ActualValue { get; set; }
}
