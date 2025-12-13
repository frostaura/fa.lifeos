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
    
    /// <summary>Background orb color 1 (purple orb) - hex color code</summary>
    public string OrbColor1 { get; set; } = "#8B5CF6";
    
    /// <summary>Background orb color 2 (cyan orb) - hex color code</summary>
    public string OrbColor2 { get; set; } = "#22D3EE";
    
    /// <summary>Background orb color 3 (pink orb) - hex color code</summary>
    public string OrbColor3 { get; set; } = "#EC4899";
    
    /// <summary>Accent color for UI elements (buttons, links, highlights) - hex color code</summary>
    public string AccentColor { get; set; } = "#8B5CF6";
    
    /// <summary>Base font size multiplier (1.0 = default, 0.875 = smaller, 1.125 = larger)</summary>
    public decimal BaseFontSize { get; set; } = 1.0m;
    
    /// <summary>Theme mode: 'dark', 'light', or 'system' for automatic</summary>
    public string ThemeMode { get; set; } = "dark";
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
}
