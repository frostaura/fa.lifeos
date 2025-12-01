using LifeOS.Domain.Common;

namespace LifeOS.Domain.Entities;

public class FinancialGoal : BaseEntity
{
    public Guid UserId { get; set; }
    
    public string Name { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; } = 0;
    
    public int Priority { get; set; } = 1; // 1 = highest
    public DateTime? TargetDate { get; set; }
    
    public string? Category { get; set; } // e.g., "Retirement", "Real Estate", "Luxury", "Experience"
    public string? IconName { get; set; } // e.g., "🏎️", "🏠", "💰", "✈️"
    
    public string Currency { get; set; } = "ZAR";
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;
    
    // Calculated property - not stored
    public decimal RemainingAmount => TargetAmount - CurrentAmount;
    public decimal ProgressPercent => TargetAmount > 0 ? (CurrentAmount / TargetAmount) * 100 : 0;
}
