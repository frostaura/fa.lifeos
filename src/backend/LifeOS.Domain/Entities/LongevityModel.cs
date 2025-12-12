using LifeOS.Domain.Common;
using LifeOS.Domain.Enums;

namespace LifeOS.Domain.Entities;

public class LongevityModel : BaseEntity
{
    public Guid? UserId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    public string InputMetrics { get; set; } = "[]";
    
    public LongevityModelType ModelType { get; set; }
    public string Parameters { get; set; } = "{}";
    
    public decimal MaxRiskReduction { get; set; }
    
    public string? SourceCitation { get; set; }
    public string? SourceUrl { get; set; }
    
    public bool IsActive { get; set; } = true;
}
