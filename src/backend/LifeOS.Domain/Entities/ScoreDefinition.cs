using LifeOS.Domain.Common;

namespace LifeOS.Domain.Entities;

public class ScoreDefinition : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    public Guid? DimensionId { get; set; }
    
    public string Formula { get; set; } = string.Empty;
    public int FormulaVersion { get; set; } = 1;
    
    public decimal MinScore { get; set; } = 0;
    public decimal MaxScore { get; set; } = 100;
    
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual Dimension? Dimension { get; set; }
    public virtual ICollection<ScoreRecord> Records { get; set; } = new List<ScoreRecord>();
}
