using LifeOS.Domain.Common;

namespace LifeOS.Domain.Entities;

/// <summary>
/// Historical record of primary stat values for a user.
/// v1.1 feature: Primary stats model (strength, wisdom, charisma, composure, energy, influence, vitality)
/// </summary>
public class PrimaryStatRecord : BaseEntity
{
    public Guid UserId { get; set; }
    
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>Physical capability, endurance (0-100)</summary>
    public int Strength { get; set; }
    
    /// <summary>Knowledge, decision quality (0-100)</summary>
    public int Wisdom { get; set; }
    
    /// <summary>Social influence, communication (0-100)</summary>
    public int Charisma { get; set; }
    
    /// <summary>Emotional regulation, stress handling (0-100)</summary>
    public int Composure { get; set; }
    
    /// <summary>Vitality, motivation levels (0-100)</summary>
    public int Energy { get; set; }
    
    /// <summary>Impact on others, leadership (0-100)</summary>
    public int Influence { get; set; }
    
    /// <summary>Overall health, longevity (0-100)</summary>
    public int Vitality { get; set; }
    
    /// <summary>
    /// JSON breakdown of how each stat was calculated from dimensions
    /// e.g., {"strength": {"health_recovery": 55, "growth_mind": 70, "weighted": 62}}
    /// </summary>
    public string? CalculationDetails { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;
}
