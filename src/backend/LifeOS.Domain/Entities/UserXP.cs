using LifeOS.Domain.Common;

namespace LifeOS.Domain.Entities;

public class UserXP : BaseEntity
{
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Total XP earned
    /// </summary>
    public long TotalXp { get; set; } = 0;
    
    /// <summary>
    /// Current level based on XP
    /// </summary>
    public int Level { get; set; } = 1;
    
    /// <summary>
    /// XP earned this week
    /// </summary>
    public int WeeklyXp { get; set; } = 0;
    
    /// <summary>
    /// Week start date for weekly XP tracking
    /// </summary>
    public DateOnly WeekStartDate { get; set; }
    
    // Navigation
    public virtual User User { get; set; } = null!;
}
