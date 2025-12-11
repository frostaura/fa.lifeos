using LifeOS.Domain.Common;

namespace LifeOS.Domain.Entities;

/// <summary>
/// Represents the user's Target Persona ("God of Mind-Power") with archetype, values, and stat targets.
/// v1.1 feature: Identity-aligned system
/// </summary>
public class IdentityProfile : BaseEntity
{
    public Guid UserId { get; set; }
    
    /// <summary>
    /// The archetype name (e.g., "God of Mind-Power", "Disciplined Achiever")
    /// </summary>
    public string Archetype { get; set; } = string.Empty;
    
    /// <summary>
    /// Detailed description of the archetype
    /// </summary>
    public string? ArchetypeDescription { get; set; }
    
    /// <summary>
    /// Core values as JSON array (e.g., ["discipline", "growth", "impact", "freedom"])
    /// </summary>
    public string Values { get; set; } = "[]";
    
    /// <summary>
    /// Primary stat targets as JSON object (e.g., {"strength": 75, "wisdom": 95, ...})
    /// Each stat is 0-100 scale
    /// </summary>
    public string PrimaryStatTargets { get; set; } = "{}";
    
    /// <summary>
    /// Linked milestone IDs as JSON array
    /// </summary>
    public string LinkedMilestoneIds { get; set; } = "[]";

    // Navigation properties
    public virtual User User { get; set; } = null!;
}
