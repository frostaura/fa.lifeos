using LifeOS.Domain.Common;

namespace LifeOS.Domain.Entities;

public class AccountProjection : BaseEntity
{
    public Guid ScenarioId { get; set; }
    public Guid AccountId { get; set; }
    
    public DateOnly PeriodDate { get; set; }
    
    public decimal Balance { get; set; }
    public decimal BalanceHomeCurrency { get; set; }
    
    public decimal PeriodIncome { get; set; } = 0;
    public decimal PeriodExpenses { get; set; } = 0;
    public decimal PeriodInterest { get; set; } = 0;
    public decimal PeriodTransfersIn { get; set; } = 0;
    public decimal PeriodTransfersOut { get; set; } = 0;
    
    public decimal? FxRateUsed { get; set; }
    public string? EventsApplied { get; set; }

    // Navigation properties
    public virtual SimulationScenario Scenario { get; set; } = null!;
    public virtual Account Account { get; set; } = null!;
}
