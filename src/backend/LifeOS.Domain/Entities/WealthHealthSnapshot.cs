using LifeOS.Domain.Common;

namespace LifeOS.Domain.Entities;

/// <summary>
/// Snapshot of Wealth Health score (0-100) with component breakdown.
/// v1.2 feature: Financial health scoring
/// </summary>
public class WealthHealthSnapshot : BaseEntity
{
    public Guid UserId { get; set; }
    
    /// <summary>When this snapshot was taken</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>Overall wealth health score (0-100)</summary>
    public decimal Score { get; set; }
    
    /// <summary>
    /// JSONB array of components: [{ componentCode, score, weight }, ...]
    /// Components: savings_rate, debt_to_income, emergency_fund, diversification, net_worth_growth
    /// </summary>
    public string Components { get; set; } = "[]";
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
}
