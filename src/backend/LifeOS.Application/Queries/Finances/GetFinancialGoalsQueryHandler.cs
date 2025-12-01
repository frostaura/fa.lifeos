using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Finances;
using LifeOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Queries.Finances;

public class GetFinancialGoalsQueryHandler 
    : IRequestHandler<GetFinancialGoalsQuery, FinancialGoalListResponse>
{
    private readonly ILifeOSDbContext _db;

    public GetFinancialGoalsQueryHandler(ILifeOSDbContext db)
    {
        _db = db;
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

        // Get total monthly investment contributions to calculate time to acquire
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

        var goalDtos = goals.Select(g => new FinancialGoalDto
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
            MonthsToAcquire = monthlyInvestmentRate > 0 
                ? (int)Math.Ceiling(g.RemainingAmount / monthlyInvestmentRate) 
                : null
        }).ToList();

        var totalTarget = goals.Sum(g => g.TargetAmount);
        var totalCurrent = goals.Sum(g => g.CurrentAmount);

        return new FinancialGoalListResponse
        {
            Goals = goalDtos,
            Summary = new FinancialGoalSummary
            {
                TotalTargetAmount = totalTarget,
                TotalCurrentAmount = totalCurrent,
                TotalRemainingAmount = totalTarget - totalCurrent,
                OverallProgressPercent = totalTarget > 0 ? (totalCurrent / totalTarget) * 100 : 0,
                MonthlyInvestmentRate = monthlyInvestmentRate
            }
        };
    }
}
