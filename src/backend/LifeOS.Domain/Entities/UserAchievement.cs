using LifeOS.Domain.Common;

namespace LifeOS.Domain.Entities;

public class UserAchievement : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid AchievementId { get; set; }
    
    /// <summary>
    /// When the achievement was unlocked
    /// </summary>
    public DateTime UnlockedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Progress toward achievement (0-100), for progressive achievements
    /// </summary>
    public int Progress { get; set; } = 100;
    
    /// <summary>
    /// Optional context about how it was earned
    /// </summary>
    public string? UnlockContext { get; set; }
    
    // Navigation
    public virtual User User { get; set; } = null!;
    public virtual Achievement Achievement { get; set; } = null!;
}
