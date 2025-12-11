using LifeOS.Application.Common.Interfaces;
using LifeOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Infrastructure.Persistence;

public class LifeOSDbContext : DbContext, ILifeOSDbContext
{
    public LifeOSDbContext(DbContextOptions<LifeOSDbContext> options) 
        : base(options)
    {
    }

    // Core entities
    public DbSet<User> Users => Set<User>();
    public DbSet<Dimension> Dimensions => Set<Dimension>();
    public DbSet<Milestone> Milestones => Set<Milestone>();
    public DbSet<LifeTask> Tasks => Set<LifeTask>();
    
    // Metrics
    public DbSet<MetricDefinition> MetricDefinitions => Set<MetricDefinition>();
    public DbSet<MetricRecord> MetricRecords => Set<MetricRecord>();
    
    // Scores & Streaks
    public DbSet<ScoreDefinition> ScoreDefinitions => Set<ScoreDefinition>();
    public DbSet<ScoreRecord> ScoreRecords => Set<ScoreRecord>();
    public DbSet<Streak> Streaks => Set<Streak>();
    
    // Financial
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<FxRate> FxRates => Set<FxRate>();
    public DbSet<IncomeSource> IncomeSources => Set<IncomeSource>();
    public DbSet<ExpenseDefinition> ExpenseDefinitions => Set<ExpenseDefinition>();
    public DbSet<TaxProfile> TaxProfiles => Set<TaxProfile>();
    public DbSet<InvestmentContribution> InvestmentContributions => Set<InvestmentContribution>();
    public DbSet<FinancialGoal> FinancialGoals => Set<FinancialGoal>();
    public DbSet<NetWorthSnapshot> NetWorthSnapshots => Set<NetWorthSnapshot>();
    
    // GameFi/Achievements
    public DbSet<Achievement> Achievements => Set<Achievement>();
    public DbSet<UserAchievement> UserAchievements => Set<UserAchievement>();
    public DbSet<UserXP> UserXPs => Set<UserXP>();
    
    // Simulation
    public DbSet<SimulationScenario> SimulationScenarios => Set<SimulationScenario>();
    public DbSet<SimulationEvent> SimulationEvents => Set<SimulationEvent>();
    public DbSet<AccountProjection> AccountProjections => Set<AccountProjection>();
    public DbSet<NetWorthProjection> NetWorthProjections => Set<NetWorthProjection>();
    
    // Health & Longevity
    public DbSet<LongevityModel> LongevityModels => Set<LongevityModel>();
    public DbSet<LongevitySnapshot> LongevitySnapshots => Set<LongevitySnapshot>();
    
    // Authentication
    public DbSet<WebAuthnCredential> WebAuthnCredentials => Set<WebAuthnCredential>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    
    // API Event Logging
    public DbSet<ApiEventLog> ApiEventLogs => Set<ApiEventLog>();
    
    // v1.1 Identity & Reviews
    public DbSet<IdentityProfile> IdentityProfiles => Set<IdentityProfile>();
    public DbSet<PrimaryStatRecord> PrimaryStatRecords => Set<PrimaryStatRecord>();
    public DbSet<ReviewSnapshot> ReviewSnapshots => Set<ReviewSnapshot>();
    public DbSet<OnboardingResponse> OnboardingResponses => Set<OnboardingResponse>();
    
    // v1.2 Enhancements
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();
    public DbSet<PrimaryStat> PrimaryStats => Set<PrimaryStat>();
    public DbSet<DimensionPrimaryStatWeight> DimensionPrimaryStatWeights => Set<DimensionPrimaryStatWeight>();
    public DbSet<TaskCompletion> TaskCompletions => Set<TaskCompletion>();
    public DbSet<HealthIndexSnapshot> HealthIndexSnapshots => Set<HealthIndexSnapshot>();
    public DbSet<AdherenceSnapshot> AdherenceSnapshots => Set<AdherenceSnapshot>();
    public DbSet<WealthHealthSnapshot> WealthHealthSnapshots => Set<WealthHealthSnapshot>();
    public DbSet<LifeOsScoreSnapshot> LifeOsScoreSnapshots => Set<LifeOsScoreSnapshot>();
    public DbSet<SimulationRun> SimulationRuns => Set<SimulationRun>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Apply all configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LifeOSDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Handle auditing (CreatedAt, UpdatedAt)
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property("CreatedAt").CurrentValue = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
