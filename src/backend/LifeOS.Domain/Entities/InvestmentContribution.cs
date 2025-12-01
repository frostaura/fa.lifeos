using LifeOS.Domain.Common;
using LifeOS.Domain.Enums;

namespace LifeOS.Domain.Entities;

public class InvestmentContribution : BaseEntity
{
    public Guid UserId { get; set; }
    
    public string Name { get; set; } = string.Empty;
    public string Currency { get; set; } = "ZAR";
    
    public decimal Amount { get; set; }
    public PaymentFrequency Frequency { get; set; } = PaymentFrequency.Monthly;
    
    public Guid? TargetAccountId { get; set; }
    public string? Category { get; set; } // e.g., "Retirement", "Emergency", "Goal-based", "Debt Repayment"
    
    public decimal? AnnualIncreaseRate { get; set; } // e.g., 0.05 for 5% annual increase
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Account? TargetAccount { get; set; }
}
