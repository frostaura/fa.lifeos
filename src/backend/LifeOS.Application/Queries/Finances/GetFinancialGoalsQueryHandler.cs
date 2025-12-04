using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Finances;
using LifeOS.Application.Services;
using LifeOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Queries.Finances;

public class GetFinancialGoalsQueryHandler 
    : IRequestHandler<GetFinancialGoalsQuery, FinancialGoalListResponse>
{
    private readonly ILifeOSDbContext _db;
    private readonly ISimulationEngine _simulationEngine;

    public GetFinancialGoalsQueryHandler(ILifeOSDbContext db, ISimulationEngine simulationEngine)
    {
        _db = db;
        _simulationEngine = simulationEngine;
    }

    public async Task<FinancialGoalListResponse> Handle(
        GetFinancialGoalsQuery query,
        CancellationToken cancellationToken)
    {
        var goals = await _db.FinancialGoals
            .Where(g => g.UserId == query.UserId && g.IsActive)
            .OrderBy(g => g.Priority)
            .ThenBy(g => g.Name)
            .ToListAsync(cancellationToken);

        // Try to get baseline scenario and use simulation for goal timing
        Dictionary<decimal, int>? goalMilestones = null;
        int? simulationBasedTotalMonths = null;
        try
        {
            var baselineScenario = await _db.SimulationScenarios
                .Where(s => s.UserId == query.UserId && s.IsBaseline)
                .FirstOrDefaultAsync(cancellationToken);

            if (baselineScenario != null && goals.Count > 0)
            {
                // Calculate total target for all goals
                var allGoalsTotalTarget = goals.Sum(g => g.TargetAmount);
                
                // Use the milestone calculation feature - include individual goals AND total
                var targetAmounts = goals
                    .Select(g => g.TargetAmount)
                    .Where(a => a > 0)
                    .Distinct()
                    .ToList();
                
                // Add total target to get milestone for ALL goals combined
                if (allGoalsTotalTarget > 0 && !targetAmounts.Contains(allGoalsTotalTarget))
                {
                    targetAmounts.Add(allGoalsTotalTarget);
                }
                    
                var milestones = await _simulationEngine.CalculateMilestonesAsync(
                    query.UserId,
                    baselineScenario.Id,
                    targetAmounts,
                    cancellationToken);

                if (milestones.Count > 0)
                {
                    goalMilestones = new Dictionary<decimal, int>();
                    foreach (var milestone in milestones)
                    {
                        if (milestone.YearsAway.HasValue && milestone.Value.HasValue)
                        {
                            var monthsAway = (int)Math.Ceiling(milestone.YearsAway.Value * 12);
                            goalMilestones[milestone.Value.Value] = monthsAway;
                            
                            // Check if this is the total target milestone
                            if (milestone.Value.Value == allGoalsTotalTarget)
                            {
                                simulationBasedTotalMonths = monthsAway;
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            // Fallback to simple calculation if simulation fails
        }

        // Fallback: Get total monthly contributions for simple calculation
        var contributions = await _db.InvestmentContributions
            .Where(c => c.UserId == query.UserId && c.IsActive)
            .ToListAsync(cancellationToken);

        var monthlyInvestmentRate = contributions.Sum(c => 
            c.Frequency == PaymentFrequency.Monthly ? c.Amount :
            c.Frequency == PaymentFrequency.Weekly ? c.Amount * 4.33m :
            c.Frequency == PaymentFrequency.Biweekly ? c.Amount * 2.167m :
            c.Frequency == PaymentFrequency.Quarterly ? c.Amount / 3m :
            c.Frequency == PaymentFrequency.Annually ? c.Amount / 12m : c.Amount
        );

        // Also include net income for more accurate simple calculation
        var incomeSources = await _db.IncomeSources
            .Where(i => i.UserId == query.UserId && i.IsActive)
            .ToListAsync(cancellationToken);

        var monthlyIncome = incomeSources.Sum(i =>
            i.PaymentFrequency == PaymentFrequency.Monthly ? i.BaseAmount :
            i.PaymentFrequency == PaymentFrequency.Weekly ? i.BaseAmount * 4.33m :
            i.PaymentFrequency == PaymentFrequency.Biweekly ? i.BaseAmount * 2.167m :
            i.PaymentFrequency == PaymentFrequency.Quarterly ? i.BaseAmount / 3m :
            i.PaymentFrequency == PaymentFrequency.Annually ? i.BaseAmount / 12m : 0
        );

        var expenses = await _db.ExpenseDefinitions
            .Where(e => e.UserId == query.UserId && e.IsActive && e.AmountValue.HasValue)
            .ToListAsync(cancellationToken);

        var monthlyExpenses = expenses
            .Where(e => e.Frequency != PaymentFrequency.Once)
            .Sum(e =>
                e.Frequency == PaymentFrequency.Monthly ? e.AmountValue!.Value :
                e.Frequency == PaymentFrequency.Weekly ? e.AmountValue!.Value * 4.33m :
                e.Frequency == PaymentFrequency.Biweekly ? e.AmountValue!.Value * 2.167m :
                e.Frequency == PaymentFrequency.Quarterly ? e.AmountValue!.Value / 3m :
                e.Frequency == PaymentFrequency.Annually ? e.AmountValue!.Value / 12m : 0
            );

        // Monthly savings = income - expenses
        var monthlySavings = monthlyIncome - monthlyExpenses;
        var effectiveMonthlySavingsRate = monthlySavings > 0 ? monthlySavings : monthlyInvestmentRate;

        var goalDtos = goals.Select(g => 
        {
            int? monthsToAcquire = null;
            
            // First try simulation-based calculation
            if (goalMilestones != null && goalMilestones.TryGetValue(g.TargetAmount, out var simMonths))
            {
                monthsToAcquire = simMonths;
            }
            // Fallback to simple calculation
            else if (effectiveMonthlySavingsRate > 0)
            {
                monthsToAcquire = (int)Math.Ceiling(g.RemainingAmount / effectiveMonthlySavingsRate);
            }

            return new FinancialGoalDto
            {
                Id = g.Id,
                Name = g.Name,
                TargetAmount = g.TargetAmount,
                CurrentAmount = g.CurrentAmount,
                Priority = g.Priority,
                TargetDate = g.TargetDate,
                Category = g.Category,
                IconName = g.IconName,
                Currency = g.Currency,
                Notes = g.Notes,
                IsActive = g.IsActive,
                CreatedAt = g.CreatedAt,
                RemainingAmount = g.RemainingAmount,
                ProgressPercent = g.ProgressPercent,
                MonthsToAcquire = monthsToAcquire
            };
        }).ToList();

        var totalTarget = goals.Sum(g => g.TargetAmount);
        var totalCurrent = goals.Sum(g => g.CurrentAmount);
        var totalRemaining = totalTarget - totalCurrent;

        // Calculate EstimatedTotalMonths for ALL goals combined
        int? estimatedTotalMonths = null;
        
        // First try: use simulation-based calculation (most accurate)
        if (simulationBasedTotalMonths.HasValue)
        {
            estimatedTotalMonths = simulationBasedTotalMonths.Value;
        }
        // Fallback: use compound growth formula
        else if (monthlyInvestmentRate > 0 && totalRemaining > 0)
        {
            // Get average annual growth rate from investment accounts
            var investmentAccounts = await _db.Accounts
                .Where(a => a.UserId == query.UserId && a.IsActive && !a.IsLiability 
                    && a.AccountType == AccountType.Investment)
                .ToListAsync(cancellationToken);
            
            var avgAnnualRate = investmentAccounts.Any() 
                ? investmentAccounts.Average(a => a.InterestRateAnnual ?? 0) / 100m 
                : 0.10m; // Default 10% if no accounts
            
            var monthlyRate = avgAnnualRate / 12m;
            
            if (monthlyRate > 0)
            {
                // Future Value of Annuity formula: FV = PMT * ((1 + r)^n - 1) / r
                // Solving for n: n = ln((FV * r / PMT) + 1) / ln(1 + r)
                // This calculates time to accumulate TOTAL remaining amount across all goals
                var targetFV = (double)totalRemaining;
                var pmt = (double)monthlyInvestmentRate;
                var r = (double)monthlyRate;
                
                var nMonths = Math.Log((targetFV * r / pmt) + 1) / Math.Log(1 + r);
                if (!double.IsNaN(nMonths) && !double.IsInfinity(nMonths) && nMonths > 0)
                {
                    estimatedTotalMonths = (int)Math.Ceiling(nMonths);
                }
            }
            
            // If compound calculation fails, fall back to simple calculation
            if (!estimatedTotalMonths.HasValue)
            {
                estimatedTotalMonths = (int)Math.Ceiling(totalRemaining / monthlyInvestmentRate);
            }
        }

        return new FinancialGoalListResponse
        {
            Goals = goalDtos,
            Summary = new FinancialGoalSummary
            {
                TotalTargetAmount = totalTarget,
                TotalCurrentAmount = totalCurrent,
                TotalRemainingAmount = totalRemaining,
                OverallProgressPercent = totalTarget > 0 ? (totalCurrent / totalTarget) * 100 : 0,
                MonthlyInvestmentRate = monthlyInvestmentRate,
                EstimatedTotalMonths = estimatedTotalMonths
            }
        };
    }
}
