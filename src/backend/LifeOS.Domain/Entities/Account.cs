using LifeOS.Domain.Common;
using LifeOS.Domain.Enums;

namespace LifeOS.Domain.Entities;

public class Account : BaseEntity
{
    public Guid UserId { get; set; }
    
    public string Name { get; set; } = string.Empty;
    public AccountType AccountType { get; set; }
    public string Currency { get; set; } = "ZAR";
    
    public decimal InitialBalance { get; set; } = 0;
    public decimal CurrentBalance { get; set; } = 0;
    public DateTime BalanceUpdatedAt { get; set; } = DateTime.UtcNow;
    
    public string? Institution { get; set; }
    
    public bool IsLiability { get; set; } = false;
    public decimal? InterestRateAnnual { get; set; }
    public CompoundingFrequency? InterestCompounding { get; set; }
    public decimal MonthlyFee { get; set; } = 0;
    
    public string Metadata { get; set; } = "{}";
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual ICollection<Transaction> SourceTransactions { get; set; } = new List<Transaction>();
    public virtual ICollection<Transaction> TargetTransactions { get; set; } = new List<Transaction>();
    public virtual ICollection<ExpenseDefinition> ExpenseDefinitions { get; set; } = new List<ExpenseDefinition>();
    public virtual ICollection<SimulationEvent> SimulationEvents { get; set; } = new List<SimulationEvent>();
    public virtual ICollection<AccountProjection> Projections { get; set; } = new List<AccountProjection>();
}
