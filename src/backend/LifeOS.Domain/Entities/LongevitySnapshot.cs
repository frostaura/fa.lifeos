using LifeOS.Domain.Common;

namespace LifeOS.Domain.Entities;

public class LongevitySnapshot : BaseEntity
{
    public Guid UserId { get; set; }
    
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    
    public decimal BaselineLifeExpectancy { get; set; }
    public decimal EstimatedYearsAdded { get; set; }
    public decimal AdjustedLifeExpectancy { get; set; }
    
    public string Breakdown { get; set; } = "{}";
    public string InputMetricsSnapshot { get; set; } = "{}";
    
    public string ConfidenceLevel { get; set; } = "moderate";

    // Navigation properties
    public virtual User User { get; set; } = null!;
}
