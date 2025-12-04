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
    
    // Start date for the expense (used for scheduling once-off expenses)
    public DateOnly? StartDate { get; set; }
    
    public string Category { get; set; } = string.Empty;
    public bool IsTaxDeductible { get; set; } = false;
    
    public Guid? LinkedAccountId { get; set; }
    
    public bool InflationAdjusted { get; set; } = true;
    public bool IsActive { get; set; } = true;
    
    // End condition properties
    public EndConditionType EndConditionType { get; set; } = EndConditionType.None;
    public Guid? EndConditionAccountId { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal? EndAmountThreshold { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Account? LinkedAccount { get; set; }
    public virtual Account? EndConditionAccount { get; set; }
}
