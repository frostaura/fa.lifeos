using LifeOS.Domain.Common;
using LifeOS.Domain.Enums;

namespace LifeOS.Domain.Entities;

public class IncomeSource : BaseEntity
{
    public Guid UserId { get; set; }
    
    public string Name { get; set; } = string.Empty;
    public string Currency { get; set; } = "ZAR";
    public decimal BaseAmount { get; set; }
    
    public bool IsPreTax { get; set; } = true;
    public Guid? TaxProfileId { get; set; }
    
    public PaymentFrequency PaymentFrequency { get; set; } = PaymentFrequency.Monthly;
    public DateOnly? NextPaymentDate { get; set; }
    
    public decimal? AnnualIncreaseRate { get; set; } = 0.05m;
    
    public string? EmployerName { get; set; }
    public string? Notes { get; set; }
    
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual TaxProfile? TaxProfile { get; set; }
}
