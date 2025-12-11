using LifeOS.Domain.Common;

namespace LifeOS.Domain.Entities;

/// <summary>
/// Comprehensive snapshot of LifeOS Score aggregating all scoring systems.
/// v1.2 feature: Master score combining health, adherence, wealth, longevity
/// </summary>
public class LifeOsScoreSnapshot : BaseEntity
{
    public Guid UserId { get; set; }
    
    /// <summary>When this snapshot was taken</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>Overall LifeOS score (0-100)</summary>
    public decimal LifeScore { get; set; }
    
    /// <summary>Health Index component (0-100)</summary>
    public decimal HealthIndex { get; set; }
    
    /// <summary>Behavioral Adherence component (0-100)</summary>
    public decimal AdherenceIndex { get; set; }
    
    /// <summary>Wealth Health component (0-100)</summary>
    public decimal WealthHealthScore { get; set; }
    
    /// <summary>Longevity years added (can be negative)</summary>
    public decimal LongevityYearsAdded { get; set; }
    
    /// <summary>
    /// JSONB array of dimension scores: [{ dimensionCode, score, weight }, ...]
    /// </summary>
    public string DimensionScores { get; set; } = "[]";
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
}
