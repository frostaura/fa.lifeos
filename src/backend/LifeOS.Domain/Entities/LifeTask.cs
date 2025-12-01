using LifeOS.Domain.Common;
using LifeOS.Domain.Enums;

namespace LifeOS.Domain.Entities;

public class LifeTask : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid? DimensionId { get; set; }
    public Guid? MilestoneId { get; set; }
    
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskType TaskType { get; set; } = TaskType.OneOff;
    public Frequency Frequency { get; set; } = Frequency.AdHoc;
    
    public DateOnly? ScheduledDate { get; set; }
    public TimeOnly? ScheduledTime { get; set; }
    
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? EndDate { get; set; }
    
    public string? LinkedMetricCode { get; set; }
    
    public bool IsCompleted { get; set; } = false;
    public DateTime? CompletedAt { get; set; }
    public bool IsActive { get; set; } = true;
    
    public string[]? Tags { get; set; }
    public string? Metadata { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Dimension? Dimension { get; set; }
    public virtual Milestone? Milestone { get; set; }
    public virtual ICollection<Streak> Streaks { get; set; } = new List<Streak>();
}
