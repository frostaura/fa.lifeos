using LifeOS.Domain.Common;
using LifeOS.Domain.Enums;

namespace LifeOS.Domain.Entities;

public class ExpenseDefinition : BaseEntity
{
    public Guid UserId { get; set; }
    
    public string Name { get; set; } = string.Empty;
    public string Currency { get; set; } = "ZAR";
    
    public AmountType AmountType { get; set; } = AmountType.Fixed;
    public decimal? AmountValue { get; set; }
    public string? AmountFormula { get; set; }
    
    public PaymentFrequency Frequency { get; set; } = PaymentFrequency.Monthly;
    
    public string Category { get; set; } = string.Empty;
    public bool IsTaxDeductible { get; set; } = false;
    
    public Guid? LinkedAccountId { get; set; }
    
    public bool InflationAdjusted { get; set; } = true;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Account? LinkedAccount { get; set; }
}
