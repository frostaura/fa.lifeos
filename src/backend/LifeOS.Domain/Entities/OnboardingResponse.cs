using LifeOS.Domain.Common;

namespace LifeOS.Domain.Entities;

/// <summary>
/// Stores user responses during goal-first onboarding.
/// v1.1 feature: Goal-First Onboarding
/// </summary>
public class OnboardingResponse : BaseEntity
{
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Step code: health_baselines, major_goals, identity
    /// </summary>
    public string StepCode { get; set; } = string.Empty;
    
    /// <summary>
    /// JSON response data for this step
    /// </summary>
    public string ResponseData { get; set; } = "{}";
    
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User User { get; set; } = null!;
}
