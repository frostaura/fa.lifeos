using LifeOS.Domain.Common;

namespace LifeOS.Domain.Entities;

/// <summary>
/// User-specific settings for currency, timezone, longevity, and simulation defaults.
/// v1.2 feature: Separated from User entity for clarity
/// </summary>
public class UserSettings : BaseEntity
{
    public Guid UserId { get; set; }
    
    /// <summary>Home currency code (ISO 4217), e.g., "ZAR", "USD"</summary>
    public string HomeCurrency { get; set; } = "ZAR";
    
    /// <summary>Timezone identifier, e.g., "Africa/Johannesburg"</summary>
    public string Timezone { get; set; } = "UTC";
    
    /// <summary>Baseline life expectancy in years (default 80)</summary>
    public decimal BaselineLifeExpectancyYears { get; set; } = 80m;
    
    /// <summary>Default annual inflation rate for simulations (e.g., 0.05 for 5%)</summary>
    public decimal DefaultInflationRate { get; set; } = 0.05m;
    
    /// <summary>Default annual investment growth rate for simulations (e.g., 0.07 for 7%)</summary>
    public decimal DefaultInvestmentGrowthRate { get; set; } = 0.07m;
    
    /// <summary>Sensitivity for streak penalty calculations: low, medium, high</summary>
    public string StreakPenaltySensitivity { get; set; } = "medium";
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
}
