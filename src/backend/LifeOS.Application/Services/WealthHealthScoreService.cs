using LifeOS.Application.Common.Interfaces;
using LifeOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Services;

public interface IWealthHealthScoreService
{
    Task<WealthHealthResult> CalculateAsync(Guid userId, CancellationToken cancellationToken = default);
}

public class WealthHealthResult
{
    public decimal OverallScore { get; set; }
    public decimal SavingsRateScore { get; set; }
    public decimal DebtToIncomeScore { get; set; }
    public decimal EmergencyFundScore { get; set; }
    public decimal DiversificationScore { get; set; }
    public decimal NetWorthGrowthScore { get; set; }
    public Dictionary<string, object> Details { get; set; } = new();
}

public class WealthHealthScoreService : IWealthHealthScoreService
{
    private readonly ILifeOSDbContext _context;
    
    // Target thresholds
    private const decimal TARGET_SAVINGS_RATE = 0.20m;  // 20%
    private const decimal MAX_DEBT_TO_INCOME = 0.36m;   // 36%
    private const int TARGET_EMERGENCY_FUND_MONTHS = 6;
    private const int MIN_ACCOUNT_TYPES_FOR_DIVERSIFICATION = 3;
    
    public WealthHealthScoreService(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<WealthHealthResult> CalculateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var result = new WealthHealthResult();
        
        // Get financial data
        var accounts = await _context.Accounts
            .Where(a => a.UserId == userId && a.IsActive)
            .ToListAsync(cancellationToken);
            
        var incomeSources = await _context.IncomeSources
            .Where(i => i.UserId == userId && i.IsActive)
            .ToListAsync(cancellationToken);
            
        var expenses = await _context.ExpenseDefinitions
            .Where(e => e.UserId == userId && e.IsActive)
            .ToListAsync(cancellationToken);
        
        // Calculate monthly income
        var monthlyIncome = incomeSources.Sum(i => ConvertToMonthly(i.BaseAmount, i.PaymentFrequency));
        
        // Calculate monthly expenses
        var monthlyExpenses = expenses.Sum(e => ConvertToMonthly(e.AmountValue ?? 0, e.Frequency));
        
        // 1. Savings Rate Score (0-100)
        // Target: 20%+ savings rate
        var savingsRate = monthlyIncome > 0 
            ? (monthlyIncome - monthlyExpenses) / monthlyIncome 
            : 0;
        result.SavingsRateScore = Math.Min(100, Math.Max(0, (savingsRate / TARGET_SAVINGS_RATE) * 100));
        result.Details["savingsRate"] = Math.Round(savingsRate * 100, 1);
        
        // 2. Debt-to-Income Score (0-100)
        // Lower is better, target < 36%
        var totalDebt = accounts.Where(a => a.IsLiability).Sum(a => a.CurrentBalance);
        var annualIncome = monthlyIncome * 12;
        var debtToIncome = annualIncome > 0 ? totalDebt / annualIncome : 0;
        result.DebtToIncomeScore = debtToIncome >= MAX_DEBT_TO_INCOME 
            ? 0 
            : Math.Max(0, (1 - (debtToIncome / MAX_DEBT_TO_INCOME)) * 100);
        result.Details["debtToIncome"] = Math.Round(debtToIncome * 100, 1);
        
        // 3. Emergency Fund Score (0-100)
        // Target: 6 months of expenses in liquid accounts
        var liquidAssets = accounts
            .Where(a => !a.IsLiability && a.AccountType == AccountType.Bank)
            .Sum(a => a.CurrentBalance);
        var emergencyFundMonths = monthlyExpenses > 0 ? liquidAssets / monthlyExpenses : 0;
        result.EmergencyFundScore = Math.Min(100, Math.Max(0, (emergencyFundMonths / TARGET_EMERGENCY_FUND_MONTHS) * 100));
        result.Details["emergencyFundMonths"] = Math.Round(emergencyFundMonths, 1);
        
        // 4. Diversification Score (0-100)
        // Based on number of account types
        var accountTypes = accounts
            .Where(a => !a.IsLiability)
            .Select(a => a.AccountType)
            .Distinct()
            .Count();
        result.DiversificationScore = Math.Min(100, 
            (accountTypes / (decimal)MIN_ACCOUNT_TYPES_FOR_DIVERSIFICATION) * 100);
        result.Details["accountTypeCount"] = accountTypes;
        
        // 5. Net Worth Growth Score (0-100)
        // Compare current net worth to 12 months ago
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var oneYearAgo = today.AddYears(-1);
        
        var currentNetWorth = accounts.Where(a => !a.IsLiability).Sum(a => a.CurrentBalance) -
                              accounts.Where(a => a.IsLiability).Sum(a => a.CurrentBalance);
        
        var historicalSnapshot = await _context.NetWorthSnapshots
            .Where(s => s.UserId == userId && s.SnapshotDate <= oneYearAgo)
            .OrderByDescending(s => s.SnapshotDate)
            .FirstOrDefaultAsync(cancellationToken);
            
        if (historicalSnapshot != null && historicalSnapshot.NetWorth > 0)
        {
            var growthRate = (currentNetWorth - historicalSnapshot.NetWorth) / historicalSnapshot.NetWorth;
            // Target: 10% annual growth = 100 score
            result.NetWorthGrowthScore = Math.Min(100, Math.Max(0, (growthRate / 0.10m) * 100));
            result.Details["netWorthGrowthPercent"] = Math.Round(growthRate * 100, 1);
        }
        else
        {
            result.NetWorthGrowthScore = 50; // Neutral if no historical data
            result.Details["netWorthGrowthPercent"] = 0;
        }
        
        // Calculate weighted overall score
        result.OverallScore = Math.Round(
            (result.SavingsRateScore * 0.25m) +
            (result.DebtToIncomeScore * 0.20m) +
            (result.EmergencyFundScore * 0.25m) +
            (result.DiversificationScore * 0.10m) +
            (result.NetWorthGrowthScore * 0.20m),
            1);
        
        return result;
    }
    
    private static decimal ConvertToMonthly(decimal amount, PaymentFrequency frequency)
    {
        return frequency switch
        {
            PaymentFrequency.Weekly => amount * 52 / 12,
            PaymentFrequency.Biweekly => amount * 26 / 12,
            PaymentFrequency.Monthly => amount,
            PaymentFrequency.Quarterly => amount / 3,
            PaymentFrequency.Annually => amount / 12,
            _ => amount
        };
    }
}
