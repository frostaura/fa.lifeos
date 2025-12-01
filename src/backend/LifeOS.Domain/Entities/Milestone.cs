using LifeOS.Domain.Common;
using LifeOS.Domain.Enums;

namespace LifeOS.Domain.Entities;

public class Milestone : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid DimensionId { get; set; }
    
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly? TargetDate { get; set; }
    public string? TargetMetricCode { get; set; }
    public decimal? TargetMetricValue { get; set; }
    
    public MilestoneStatus Status { get; set; } = MilestoneStatus.Active;
    public DateTime? CompletedAt { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Dimension Dimension { get; set; } = null!;
    public virtual ICollection<LifeTask> Tasks { get; set; } = new List<LifeTask>();
}
