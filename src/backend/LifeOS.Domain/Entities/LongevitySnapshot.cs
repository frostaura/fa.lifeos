using LifeOS.Domain.Common;

namespace LifeOS.Domain.Entities;

public class LongevitySnapshot : BaseEntity
{
    public Guid UserId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public decimal BaselineLifeExpectancyYears { get; set; }
    public decimal AdjustedLifeExpectancyYears { get; set; }
    public decimal TotalYearsAdded { get; set; }
    public decimal RiskFactorCombined { get; set; }
    
    public string Breakdown { get; set; } = "[]";  // JSON of LongevityComponent[]
    public string Confidence { get; set; } = "medium";  // low/medium/high

    // Navigation properties
    public virtual User User { get; set; } = null!;
}
