using LifeOS.Domain.Common;

namespace LifeOS.Domain.Entities;

/// <summary>
/// Links dimensions to primary stats with weights (0-1).
/// v1.2 feature: Allows each dimension to contribute to multiple primary stats
/// </summary>
public class DimensionPrimaryStatWeight : BaseEntity
{
    public Guid DimensionId { get; set; }
    public string PrimaryStatCode { get; set; } = string.Empty;
    
    /// <summary>Weight value 0-1, sum per dimension should be <= 1</summary>
    public decimal Weight { get; set; }
    
    // Navigation properties
    public virtual Dimension Dimension { get; set; } = null!;
    public virtual PrimaryStat PrimaryStat { get; set; } = null!;
}
