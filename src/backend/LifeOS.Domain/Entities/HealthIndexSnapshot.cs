using LifeOS.Domain.Common;

namespace LifeOS.Domain.Entities;

/// <summary>
/// Snapshot of Health Index score (0-100) with per-metric breakdown.
/// v1.2 feature: Separate health scoring
/// </summary>
public class HealthIndexSnapshot : BaseEntity
{
    public Guid UserId { get; set; }
    
    /// <summary>When this snapshot was taken</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>Overall health index score (0-100)</summary>
    public decimal Score { get; set; }
    
    /// <summary>
    /// JSONB array of components: [{ metricCode, score, weight }, ...]
    /// </summary>
    public string Components { get; set; } = "[]";
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
}
