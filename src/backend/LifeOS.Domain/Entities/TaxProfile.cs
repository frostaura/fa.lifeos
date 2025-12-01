using LifeOS.Domain.Common;

namespace LifeOS.Domain.Entities;

public class TaxProfile : BaseEntity
{
    public Guid UserId { get; set; }
    
    public string Name { get; set; } = "Default";
    public int TaxYear { get; set; }
    public string CountryCode { get; set; } = "ZA";
    
    public string Brackets { get; set; } = "[]";
    
    public decimal? UifRate { get; set; } = 0.01m;
    public decimal? UifCap { get; set; } = 177.12m; // Monthly contribution cap (R17,712 income ceiling × 1%)
    
    public decimal? VatRate { get; set; } = 0.15m;
    public bool IsVatRegistered { get; set; } = false;
    
    public string? TaxRebates { get; set; }
    public string? MedicalCredits { get; set; }
    
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual ICollection<IncomeSource> IncomeSources { get; set; } = new List<IncomeSource>();
}
