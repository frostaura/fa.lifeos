using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Finances;
using LifeOS.Domain.Entities;
using MediatR;

namespace LifeOS.Application.Commands.Finances;

public record CreateFinancialGoalCommand(
    Guid UserId,
    CreateFinancialGoalRequest Request
) : IRequest<FinancialGoalDto>;

public class CreateFinancialGoalCommandHandler 
    : IRequestHandler<CreateFinancialGoalCommand, FinancialGoalDto>
{
    private readonly ILifeOSDbContext _db;

    public CreateFinancialGoalCommandHandler(ILifeOSDbContext db)
    {
        _db = db;
    }

    public async Task<FinancialGoalDto> Handle(
        CreateFinancialGoalCommand command,
        CancellationToken cancellationToken)
    {
        // Convert TargetDate to UTC if provided
        DateTime? targetDateUtc = command.Request.TargetDate.HasValue
            ? DateTime.SpecifyKind(command.Request.TargetDate.Value, DateTimeKind.Utc)
            : null;
        
        var goal = new FinancialGoal
        {
            UserId = command.UserId,
            Name = command.Request.Name,
            TargetAmount = command.Request.TargetAmount,
            CurrentAmount = command.Request.CurrentAmount,
            Priority = command.Request.Priority,
            TargetDate = targetDateUtc,
            Category = command.Request.Category,
            IconName = command.Request.IconName,
            Currency = command.Request.Currency,
            Notes = command.Request.Notes,
            IsActive = true,
        };

        _db.FinancialGoals.Add(goal);
        await _db.SaveChangesAsync(cancellationToken);

        return new FinancialGoalDto
        {
            Id = goal.Id,
            Name = goal.Name,
            TargetAmount = goal.TargetAmount,
            CurrentAmount = goal.CurrentAmount,
            Priority = goal.Priority,
            TargetDate = goal.TargetDate,
            Category = goal.Category,
            IconName = goal.IconName,
            Currency = goal.Currency,
            Notes = goal.Notes,
            IsActive = goal.IsActive,
            CreatedAt = goal.CreatedAt,
            RemainingAmount = goal.RemainingAmount,
            ProgressPercent = goal.ProgressPercent,
            MonthsToAcquire = null // Will be calculated on list query
        };
    }
}
