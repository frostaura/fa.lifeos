using LifeOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace LifeOS.Application.Common.Interfaces;

public interface ILifeOSDbContext
{
    // Core entities
    DbSet<User> Users { get; }
    DbSet<Dimension> Dimensions { get; }
    DbSet<Milestone> Milestones { get; }
    DbSet<LifeTask> Tasks { get; }
    
    // Metrics
    DbSet<MetricDefinition> MetricDefinitions { get; }
    DbSet<MetricRecord> MetricRecords { get; }
    
    // Scores & Streaks
    DbSet<ScoreDefinition> ScoreDefinitions { get; }
    DbSet<ScoreRecord> ScoreRecords { get; }
    DbSet<Streak> Streaks { get; }
    
    // Financial
    DbSet<Account> Accounts { get; }
    DbSet<Transaction> Transactions { get; }
    DbSet<FxRate> FxRates { get; }
    DbSet<IncomeSource> IncomeSources { get; }
    DbSet<ExpenseDefinition> ExpenseDefinitions { get; }
    DbSet<TaxProfile> TaxProfiles { get; }
    DbSet<InvestmentContribution> InvestmentContributions { get; }
    DbSet<FinancialGoal> FinancialGoals { get; }
    DbSet<NetWorthSnapshot> NetWorthSnapshots { get; }
    
    // GameFi/Achievements
    DbSet<Achievement> Achievements { get; }
    DbSet<UserAchievement> UserAchievements { get; }
    DbSet<UserXP> UserXPs { get; }
    
    // Simulation
    DbSet<SimulationScenario> SimulationScenarios { get; }
    DbSet<SimulationEvent> SimulationEvents { get; }
    DbSet<AccountProjection> AccountProjections { get; }
    DbSet<NetWorthProjection> NetWorthProjections { get; }
    
    // Health & Longevity
    DbSet<LongevityModel> LongevityModels { get; }
    DbSet<LongevitySnapshot> LongevitySnapshots { get; }
    
    // Authentication
    DbSet<ApiKey> ApiKeys { get; }
    
    // API Event Logging
    DbSet<ApiEventLog> ApiEventLogs { get; }
    
    // v1.1 Identity & Reviews
    DbSet<IdentityProfile> IdentityProfiles { get; }
    DbSet<PrimaryStatRecord> PrimaryStatRecords { get; }
    DbSet<ReviewSnapshot> ReviewSnapshots { get; }
    DbSet<OnboardingResponse> OnboardingResponses { get; }
    
    // v1.2 Enhancements
    DbSet<UserSettings> UserSettings { get; }
    DbSet<PrimaryStat> PrimaryStats { get; }
    DbSet<DimensionPrimaryStatWeight> DimensionPrimaryStatWeights { get; }
    DbSet<TaskCompletion> TaskCompletions { get; }
    DbSet<HealthIndexSnapshot> HealthIndexSnapshots { get; }
    DbSet<AdherenceSnapshot> AdherenceSnapshots { get; }
    DbSet<WealthHealthSnapshot> WealthHealthSnapshots { get; }
    DbSet<LifeOsScoreSnapshot> LifeOsScoreSnapshots { get; }
    DbSet<SimulationRun> SimulationRuns { get; }
    
    // Change tracker for concurrency handling
    ChangeTracker ChangeTracker { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
