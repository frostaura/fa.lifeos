using LifeOS.Application.Common.Interfaces;
using LifeOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Services;

/// <summary>
/// v1.2: Wealth Health calculation service
/// Calculates financial health score from multiple components
/// </summary>
public interface IWealthHealthService
{
    Task<WealthHealthSnapshot> CalculateWealthHealthAsync(Guid userId, CancellationToken cancellationToken = default);
}

public class WealthHealthService : IWealthHealthService
{
    private readonly ILifeOSDbContext _context;

    public WealthHealthService(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<WealthHealthSnapshot> CalculateWealthHealthAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var components = new List<object>();
        
        // Get user's accounts
        var accounts = await _context.Accounts
            .Where(a => a.UserId == userId && a.IsActive)
            .ToListAsync(cancellationToken);

        // Component 1: Net Worth Growth
        var netWorthScore = await CalculateNetWorthGrowthScore(userId, cancellationToken);
        components.Add(new
        {
            componentCode = "net_worth_growth",
            score = netWorthScore,
            weight = 0.3m
        });

        // Component 2: Savings Rate
        var savingsScore = await CalculateSavingsRateScore(userId, cancellationToken);
        components.Add(new
        {
            componentCode = "savings_rate",
            score = savingsScore,
            weight = 0.25m
        });

        // Component 3: Debt to Income
        var debtScore = await CalculateDebtToIncomeScore(userId, accounts, cancellationToken);
        components.Add(new
        {
            componentCode = "debt_to_income",
            score = debtScore,
            weight = 0.25m
        });

        // Component 4: Emergency Fund
        var emergencyScore = CalculateEmergencyFundScore(accounts);
        components.Add(new
        {
            componentCode = "emergency_fund",
            score = emergencyScore,
            weight = 0.1m
        });

        // Component 5: Diversification
        var diversificationScore = CalculateDiversificationScore(accounts);
        components.Add(new
        {
            componentCode = "diversification",
            score = diversificationScore,
            weight = 0.1m
        });

        // Calculate weighted average
        decimal totalScore = 0;
        decimal totalWeight = 0;
        foreach (dynamic component in components)
        {
            totalScore += component.score * component.weight;
            totalWeight += component.weight;
        }

        var finalScore = totalWeight > 0 ? totalScore / totalWeight : 50m;

        var snapshot = new WealthHealthSnapshot
        {
            UserId = userId,
            Score = Math.Round(finalScore, 2),
            Components = System.Text.Json.JsonSerializer.Serialize(components)
        };

        _context.WealthHealthSnapshots.Add(snapshot);
        await _context.SaveChangesAsync(cancellationToken);

        return snapshot;
    }

    private async Task<decimal> CalculateNetWorthGrowthScore(Guid userId, CancellationToken cancellationToken)
    {
        var snapshots = await _context.NetWorthSnapshots
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.SnapshotDate)
            .Take(12)
            .ToListAsync(cancellationToken);

        if (snapshots.Count < 2)
            return 50m;

        var latest = snapshots.First().NetWorth;
        var oldest = snapshots.Last().NetWorth;
        
        if (oldest == 0)
            return latest > 0 ? 100m : 50m;

        var growthRate = (latest - oldest) / Math.Abs(oldest);
        
        // Score based on growth rate: 10% = 100, 5% = 75, 0% = 50, negative = below 50
        if (growthRate >= 0.1m) return 100m;
        if (growthRate >= 0.05m) return 75m + 25m * (growthRate - 0.05m) / 0.05m;
        if (growthRate >= 0) return 50m + 25m * growthRate / 0.05m;
        return Math.Max(0, 50m + 50m * growthRate); // Penalty for negative growth
    }

    private async Task<decimal> CalculateSavingsRateScore(Guid userId, CancellationToken cancellationToken)
    {
        // Get income and expenses from last 3 months
        var threeMonthsAgo = DateTime.UtcNow.AddMonths(-3);
        
        var totalIncome = await _context.Transactions
            .Where(t => t.UserId == userId && t.RecordedAt >= threeMonthsAgo && t.Category == Domain.Enums.TransactionCategory.Income)
            .SumAsync(t => t.Amount, cancellationToken);

        var totalExpenses = await _context.Transactions
            .Where(t => t.UserId == userId && t.RecordedAt >= threeMonthsAgo && t.Category == Domain.Enums.TransactionCategory.Expense)
            .SumAsync(t => t.Amount, cancellationToken);

        if (totalIncome == 0)
            return 50m;

        var savingsRate = (totalIncome - totalExpenses) / totalIncome;
        
        // Score: 30% savings = 100, 20% = 85, 10% = 70, 0% = 50
        if (savingsRate >= 0.3m) return 100m;
        if (savingsRate >= 0.2m) return 85m + 15m * (savingsRate - 0.2m) / 0.1m;
        if (savingsRate >= 0.1m) return 70m + 15m * (savingsRate - 0.1m) / 0.1m;
        if (savingsRate >= 0) return 50m + 20m * savingsRate / 0.1m;
        return Math.Max(0, 50m + 50m * savingsRate);
    }

    private async Task<decimal> CalculateDebtToIncomeScore(Guid userId, List<Account> accounts, CancellationToken cancellationToken)
    {
        var totalDebt = accounts.Where(a => a.IsLiability).Sum(a => Math.Abs(a.CurrentBalance));
        
        var monthlyIncome = await _context.IncomeSources
            .Where(i => i.UserId == userId && i.IsActive)
            .SumAsync(i => i.BaseAmount, cancellationToken);

        if (monthlyIncome == 0)
            return totalDebt == 0 ? 100m : 0m;

        var debtToIncome = totalDebt / (monthlyIncome * 12); // Annual income
        
        // Score: 0% = 100, 20% = 85, 35% = 60, 50%+ = 0
        if (debtToIncome == 0) return 100m;
        if (debtToIncome <= 0.2m) return 85m + 15m * (0.2m - debtToIncome) / 0.2m;
        if (debtToIncome <= 0.35m) return 60m + 25m * (0.35m - debtToIncome) / 0.15m;
        if (debtToIncome <= 0.5m) return 60m * (0.5m - debtToIncome) / 0.15m;
        return 0m;
    }

    private decimal CalculateEmergencyFundScore(List<Account> accounts)
    {
        var liquidAssets = accounts
            .Where(a => !a.IsLiability && a.AccountType == Domain.Enums.AccountType.Bank)
            .Sum(a => a.CurrentBalance);

        // Assume $3000/month expenses minimum
        var monthsCovered = liquidAssets / 3000m;
        
        // Score: 6+ months = 100, 3 months = 75, 1 month = 50
        if (monthsCovered >= 6) return 100m;
        if (monthsCovered >= 3) return 75m + 25m * (monthsCovered - 3) / 3;
        if (monthsCovered >= 1) return 50m + 25m * (monthsCovered - 1) / 2;
        return 50m * monthsCovered;
    }

    private decimal CalculateDiversificationScore(List<Account> accounts)
    {
        var totalAssets = accounts.Where(a => !a.IsLiability).Sum(a => a.CurrentBalance);
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
}
