using LifeOS.Domain.Common;
using LifeOS.Domain.Enums;

namespace LifeOS.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? PasswordHash { get; set; }  // Nullable for biometric-only users
    
    public string HomeCurrency { get; set; } = "ZAR";
    public DateOnly? DateOfBirth { get; set; }
    public decimal LifeExpectancyBaseline { get; set; } = 80;
    
    public string DefaultAssumptions { get; set; } = @"{
        ""inflationRateAnnual"": 0.05,
        ""defaultGrowthRate"": 0.07,
        ""retirementAge"": 65
    }";
    
    public UserRole Role { get; set; } = UserRole.User;
    public UserStatus Status { get; set; } = UserStatus.Active;

    // Navigation properties
    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();
    public virtual ICollection<Milestone> Milestones { get; set; } = new List<Milestone>();
    public virtual ICollection<LifeTask> Tasks { get; set; } = new List<LifeTask>();
    public virtual ICollection<MetricRecord> MetricRecords { get; set; } = new List<MetricRecord>();
    public virtual ICollection<ScoreRecord> ScoreRecords { get; set; } = new List<ScoreRecord>();
    public virtual ICollection<Streak> Streaks { get; set; } = new List<Streak>();
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public virtual ICollection<IncomeSource> IncomeSources { get; set; } = new List<IncomeSource>();
    public virtual ICollection<ExpenseDefinition> ExpenseDefinitions { get; set; } = new List<ExpenseDefinition>();
    public virtual ICollection<TaxProfile> TaxProfiles { get; set; } = new List<TaxProfile>();
    public virtual ICollection<SimulationScenario> SimulationScenarios { get; set; } = new List<SimulationScenario>();
    public virtual ICollection<LongevitySnapshot> LongevitySnapshots { get; set; } = new List<LongevitySnapshot>();
    public virtual ICollection<WebAuthnCredential> WebAuthnCredentials { get; set; } = new List<WebAuthnCredential>();
    public virtual ICollection<ApiKey> ApiKeys { get; set; } = new List<ApiKey>();
}
