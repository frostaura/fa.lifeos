using System.Text.Json;
using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.Interfaces;
using LifeOS.Domain.Entities;
using LifeOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LifeOS.Application.Services;

/// <summary>
/// v3.0: Wealth Health Calculator with 5-component scoring
/// Calculates financial health from savings, debt, emergency fund, diversification, and net worth growth
/// </summary>
public class WealthHealthCalculator : IWealthHealthCalculator
{
    private readonly ILifeOSDbContext _context;
    private readonly ILogger<WealthHealthCalculator> _logger;

    // Default weights per design spec
    private const decimal WEIGHT_SAVINGS_RATE = 0.25m;
    private const decimal WEIGHT_DEBT_TO_INCOME = 0.20m;
    private const decimal WEIGHT_EMERGENCY_FUND = 0.20m;
    private const decimal WEIGHT_DIVERSIFICATION = 0.15m;
    private const decimal WEIGHT_NET_WORTH_GROWTH = 0.20m;

    public WealthHealthCalculator(
        ILifeOSDbContext context,
        ILogger<WealthHealthCalculator> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<WealthHealthCalculation> CalculateAsync(
        Guid userId, 
        DateTime? asOfDate = null, 
        CancellationToken cancellationToken = default)
    {
        var evaluationDate = asOfDate ?? DateTime.UtcNow;
        
        _logger.LogInformation("Calculating Wealth Health for user {UserId} as of {Date}", 
            userId, evaluationDate);
        
        var components = new List<WealthComponent>();
        
        // Load user's accounts once
        var accounts = await _context.Accounts
            .AsNoTracking()
            .Where(a => a.UserId == userId && a.IsActive)
            .ToListAsync(cancellationToken);
        
        // Component 1: Savings Rate
        var savingsRateData = await CalculateSavingsRateAsync(userId, evaluationDate, cancellationToken);
        if (savingsRateData.HasValue)
        {
            components.Add(new WealthComponent
            {
                ComponentCode = "savings_rate",
                Score = ScoreSavingsRate(savingsRateData.Value),
                Weight = WEIGHT_SAVINGS_RATE,
                ActualValue = savingsRateData.Value
            });
            _logger.LogDebug("Savings Rate: {Rate}% -> Score {Score}", 
                Math.Round(savingsRateData.Value * 100, 2), 
                Math.Round(components.Last().Score, 2));
        }
        
        // Component 2: Debt-to-Income
        var debtToIncomeData = await CalculateDebtToIncomeAsync(userId, accounts, evaluationDate, cancellationToken);
        if (debtToIncomeData.HasValue)
        {
            components.Add(new WealthComponent
            {
                ComponentCode = "debt_to_income",
                Score = ScoreDebtToIncome(debtToIncomeData.Value),
                Weight = WEIGHT_DEBT_TO_INCOME,
                ActualValue = debtToIncomeData.Value
            });
            _logger.LogDebug("Debt-to-Income: {Ratio}% -> Score {Score}", 
                Math.Round(debtToIncomeData.Value * 100, 2), 
                Math.Round(components.Last().Score, 2));
        }
        
        // Component 3: Emergency Fund
        var emergencyFundData = CalculateEmergencyFundMonths(accounts, userId);
        if (emergencyFundData.HasValue)
        {
            components.Add(new WealthComponent
            {
                ComponentCode = "emergency_fund",
                Score = ScoreEmergencyFund(emergencyFundData.Value),
                Weight = WEIGHT_EMERGENCY_FUND,
                ActualValue = emergencyFundData.Value
            });
            _logger.LogDebug("Emergency Fund: {Months} months -> Score {Score}", 
                Math.Round(emergencyFundData.Value, 2), 
                Math.Round(components.Last().Score, 2));
        }
        
        // Component 4: Diversification
        var diversificationData = CalculateDiversification(accounts);
        components.Add(new WealthComponent
        {
            ComponentCode = "diversification",
            Score = diversificationData,
            Weight = WEIGHT_DIVERSIFICATION,
            ActualValue = diversificationData
        });
        _logger.LogDebug("Diversification: Score {Score}", Math.Round(diversificationData, 2));
        
        // Component 5: Net Worth Growth
        var netWorthGrowthData = await CalculateNetWorthGrowthAsync(userId, evaluationDate, cancellationToken);
        if (netWorthGrowthData.HasValue)
        {
            components.Add(new WealthComponent
            {
                ComponentCode = "net_worth_growth",
                Score = ScoreNetWorthGrowth(netWorthGrowthData.Value),
                Weight = WEIGHT_NET_WORTH_GROWTH,
                ActualValue = netWorthGrowthData.Value
            });
            _logger.LogDebug("Net Worth Growth: {Growth}% -> Score {Score}", 
                Math.Round(netWorthGrowthData.Value * 100, 2), 
                Math.Round(components.Last().Score, 2));
        }
        
        // Calculate weighted average
        var totalWeight = components.Sum(c => c.Weight);
        var weightedScore = totalWeight > 0 
            ? components.Sum(c => c.Score * c.Weight) / totalWeight
            : 0m;
        
        _logger.LogInformation("Wealth Health calculated: {Score}/100 from {ComponentCount} components (total weight: {TotalWeight})",
            Math.Round(weightedScore, 2), components.Count, Math.Round(totalWeight, 2));
        
        return new WealthHealthCalculation
        {
            Score = Math.Round(weightedScore, 2),
            Components = components,
            CalculatedAt = DateTime.UtcNow
        };
    }
    
    /// <summary>
    /// Calculate savings rate from income and expenses over the last 3 months
    /// </summary>
    private async Task<decimal?> CalculateSavingsRateAsync(
        Guid userId, 
        DateTime asOfDate, 
        CancellationToken cancellationToken)
    {
        var threeMonthsAgo = asOfDate.AddMonths(-3);
        
        var totalIncome = await _context.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId 
                && t.RecordedAt >= threeMonthsAgo 
                && t.RecordedAt <= asOfDate
                && t.Category == TransactionCategory.Income)
            .SumAsync(t => t.Amount, cancellationToken);

        var totalExpenses = await _context.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId 
                && t.RecordedAt >= threeMonthsAgo 
                && t.RecordedAt <= asOfDate
                && t.Category == TransactionCategory.Expense)
            .SumAsync(t => t.Amount, cancellationToken);

        if (totalIncome == 0)
            return null;

        return (totalIncome - totalExpenses) / totalIncome;
    }
    
    /// <summary>
    /// Calculate debt-to-income ratio
    /// </summary>
    private async Task<decimal?> CalculateDebtToIncomeAsync(
        Guid userId, 
        List<Account> accounts, 
        DateTime asOfDate, 
        CancellationToken cancellationToken)
    {
        var totalDebt = accounts
            .Where(a => a.IsLiability)
            .Sum(a => Math.Abs(a.CurrentBalance));
        
        var monthlyIncome = await _context.IncomeSources
            .AsNoTracking()
            .Where(i => i.UserId == userId && i.IsActive)
            .SumAsync(i => i.BaseAmount, cancellationToken);

        if (monthlyIncome == 0)
            return totalDebt == 0 ? 0m : (decimal?)null;

        // Debt-to-income is annual debt / annual income
        return totalDebt / (monthlyIncome * 12);
    }
    
    /// <summary>
    /// Calculate emergency fund coverage in months
    /// </summary>
    private decimal? CalculateEmergencyFundMonths(List<Account> accounts, Guid userId)
    {
        var liquidAssets = accounts
            .Where(a => !a.IsLiability 
                && (a.AccountType == AccountType.Bank || a.AccountType == AccountType.Investment))
            .Sum(a => a.CurrentBalance);

        // Get monthly expenses estimate from last 3 months
        // For now, use a default of 3000 if no expense data
        // In production, this should calculate from actual expenses
        var estimatedMonthlyExpenses = 3000m; // TODO: Calculate from actual expenses
        
        if (estimatedMonthlyExpenses == 0)
            return null;
        
        return liquidAssets / estimatedMonthlyExpenses;
    }
    
    /// <summary>
    /// Calculate diversification score based on asset allocation spread
    /// </summary>
    private decimal CalculateDiversification(List<Account> accounts)
    {
        var totalAssets = accounts
            .Where(a => !a.IsLiability && a.CurrentBalance > 0)
            .Sum(a => a.CurrentBalance);
        
        if (totalAssets == 0)
            return 50m;

        var accountTypes = accounts
            .Where(a => !a.IsLiability && a.CurrentBalance > 0)
            .Select(a => a.AccountType)
            .Distinct()
            .Count();

        // Score based on number of different asset types
        // 1 type = 40, 2 types = 60, 3 types = 80, 4+ types = 100
        return accountTypes switch
        {
            >= 4 => 100m,
            3 => 80m,
            2 => 60m,
            1 => 40m,
            _ => 20m
        };
    }
    
    /// <summary>
    /// Calculate net worth growth rate over the last 12 months
    /// </summary>
    private async Task<decimal?> CalculateNetWorthGrowthAsync(
        Guid userId, 
        DateTime asOfDate, 
        CancellationToken cancellationToken)
    {
        var asOfDateOnly = DateOnly.FromDateTime(asOfDate);
        var twelveMonthsAgo = asOfDateOnly.AddMonths(-12);
        
        var snapshots = await _context.NetWorthSnapshots
            .AsNoTracking()
            .Where(s => s.UserId == userId 
                && s.SnapshotDate <= asOfDateOnly
                && s.SnapshotDate >= twelveMonthsAgo)
            .OrderByDescending(s => s.SnapshotDate)
            .Take(2)
            .ToListAsync(cancellationToken);

        if (snapshots.Count < 2)
            return null;

        var latest = snapshots.First().NetWorth;
        var oldest = snapshots.Last().NetWorth;
        
        if (oldest == 0)
            return latest > 0 ? 1m : 0m; // 100% growth if starting from 0

        return (latest - oldest) / Math.Abs(oldest);
    }

    #region Scoring Functions

    /// <summary>
    /// Score savings rate: Target 30%
    /// </summary>
    protected virtual decimal ScoreSavingsRate(decimal actualRate)
    {
        const decimal target = 0.30m; // 30%
        return Math.Clamp((actualRate / target) * 100, 0, 100);
    }

    /// <summary>
    /// Score debt-to-income: Target ≤ 30%, linear decay to 0 at 100%
    /// </summary>
    protected virtual decimal ScoreDebtToIncome(decimal ratio)
    {
        const decimal goodThreshold = 0.30m; // 30%
        const decimal badThreshold = 1.00m;   // 100%
        
        if (ratio <= goodThreshold) return 100m;
        if (ratio >= badThreshold) return 0m;
        
        return ((badThreshold - ratio) / (badThreshold - goodThreshold)) * 100;
    }

    /// <summary>
    /// Score emergency fund: Target 6 months
    /// </summary>
    protected virtual decimal ScoreEmergencyFund(decimal months)
    {
        const decimal target = 6m;
        return Math.Clamp((months / target) * 100, 0, 100);
    }

    /// <summary>
    /// Score net worth growth: Target 8% (inflation + 5%)
    /// </summary>
    protected virtual decimal ScoreNetWorthGrowth(decimal growthRate)
    {
        const decimal target = 0.08m; // 8%
        return Math.Clamp((growthRate / target) * 100, 0, 100);
    }

    #endregion

    public async Task<WealthHealthSnapshot> SaveSnapshotAsync(
        WealthHealthCalculation result, 
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        var snapshot = new WealthHealthSnapshot
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Timestamp = result.CalculatedAt,
            Score = result.Score,
            Components = JsonSerializer.Serialize(result.Components),
            CreatedAt = DateTime.UtcNow
        };
        
        _context.WealthHealthSnapshots.Add(snapshot);
        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Saved Wealth Health snapshot {SnapshotId} for user {UserId} with score {Score}",
            snapshot.Id, userId, snapshot.Score);
        
        return snapshot;
    }
}
