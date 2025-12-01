using LifeOS.Domain.Common;
using LifeOS.Domain.Enums;

namespace LifeOS.Domain.Entities;

public class Transaction : BaseEntity
{
    public Guid UserId { get; set; }
    
    public Guid? SourceAccountId { get; set; }
    public Guid? TargetAccountId { get; set; }
    
    public string Currency { get; set; } = "ZAR";
    public decimal Amount { get; set; }
    public decimal? AmountHomeCurrency { get; set; }
    public decimal? FxRateUsed { get; set; }
    
    public TransactionCategory Category { get; set; }
    public string? Subcategory { get; set; }
    public string[]? Tags { get; set; }
    
    public string? Description { get; set; }
    public string? Notes { get; set; }
    
    public DateOnly TransactionDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    
    public string Source { get; set; } = "manual";
    public string? ExternalId { get; set; }
    
    public bool IsReconciled { get; set; } = false;
    public string? Metadata { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Account? SourceAccount { get; set; }
    public virtual Account? TargetAccount { get; set; }
}
