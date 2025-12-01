using LifeOS.Domain.Common;

namespace LifeOS.Domain.Entities;

public class MetricRecord : BaseEntity
{
    public Guid UserId { get; set; }
    public string MetricCode { get; set; } = string.Empty;
    
    public decimal? ValueNumber { get; set; }
    public bool? ValueBoolean { get; set; }
    public string? ValueString { get; set; }
    
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    
    public string Source { get; set; } = "manual";
    public string? Notes { get; set; }
    public string? Metadata { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual MetricDefinition MetricDefinition { get; set; } = null!;
}
