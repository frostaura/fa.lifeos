using LifeOS.Domain.Common;

namespace LifeOS.Domain.Entities;

public class LongevityModel : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    public string[] InputMetrics { get; set; } = Array.Empty<string>();
    
    public string ModelType { get; set; } = "linear";
    public string Parameters { get; set; } = "{}";
    
    public string OutputUnit { get; set; } = "years_added";
    
    public string? SourceCitation { get; set; }
    public string? SourceUrl { get; set; }
    
    public bool IsActive { get; set; } = true;
    public int Version { get; set; } = 1;
}
