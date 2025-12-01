using LifeOS.Domain.Common;

namespace LifeOS.Domain.Entities;

public class Achievement : BaseEntity
{
    /// <summary>
    /// Unique code for the achievement (e.g., "first_million", "streak_30_days")
    /// </summary>
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// Display name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Description of how to earn the achievement
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Icon identifier (lucide icon name or URL)
    /// </summary>
    public string Icon { get; set; } = string.Empty;
    
    /// <summary>
    /// XP value awarded when achievement is unlocked
    /// </summary>
    public int XpValue { get; set; } = 0;
    
    /// <summary>
    /// Achievement category (financial, health, streak, milestone)
    /// </summary>
    public string Category { get; set; } = string.Empty;
    
    /// <summary>
    /// Tier level (bronze, silver, gold, platinum, diamond)
    /// </summary>
    public string Tier { get; set; } = "bronze";
    
    /// <summary>
    /// Condition expression to evaluate for unlocking
    /// </summary>
    public string UnlockCondition { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether the achievement is currently available
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Sort order for display
    /// </summary>
    public int SortOrder { get; set; } = 0;
    
    // Navigation
    public virtual ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();
}
