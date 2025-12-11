using LifeOS.Domain.Common;

namespace LifeOS.Domain.Entities;

/// <summary>
/// Definition of a primary stat (strength, wisdom, charisma, composure, energy, influence, vitality).
/// v1.2 feature: Separate definition table for primary stats
/// </summary>
public class PrimaryStat : BaseEntity
{
    /// <summary>Unique code for the stat (e.g., "strength", "wisdom")</summary>
    public string Code { get; set; } = string.Empty;
    
    /// <summary>Display name</summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>Description of what this stat represents</summary>
    public string? Description { get; set; }
    
    /// <summary>Sort order for display</summary>
    public short SortOrder { get; set; } = 0;
    
    /// <summary>Whether this stat is active</summary>
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public virtual ICollection<DimensionPrimaryStatWeight> DimensionWeights { get; set; } = new List<DimensionPrimaryStatWeight>();
}
