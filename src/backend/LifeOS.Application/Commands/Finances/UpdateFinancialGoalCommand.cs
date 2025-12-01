using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Finances;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Commands.Finances;

public record UpdateFinancialGoalCommand(
    Guid UserId,
    Guid GoalId,
    UpdateFinancialGoalRequest Request
) : IRequest<FinancialGoalDto?>;

public class UpdateFinancialGoalCommandHandler 
    : IRequestHandler<UpdateFinancialGoalCommand, FinancialGoalDto?>
{
    private readonly ILifeOSDbContext _db;

    public UpdateFinancialGoalCommandHandler(ILifeOSDbContext db)
    {
        _db = db;
    }

    public async Task<FinancialGoalDto?> Handle(
        UpdateFinancialGoalCommand command,
        CancellationToken cancellationToken)
    {
        var goal = await _db.FinancialGoals
            .FirstOrDefaultAsync(g => g.Id == command.GoalId && g.UserId == command.UserId, cancellationToken);

        if (goal == null)
            return null;

        if (command.Request.Name != null)
            goal.Name = command.Request.Name;
        if (command.Request.TargetAmount.HasValue)
            goal.TargetAmount = command.Request.TargetAmount.Value;
        if (command.Request.CurrentAmount.HasValue)
            goal.CurrentAmount = command.Request.CurrentAmount.Value;
        if (command.Request.Priority.HasValue)
            goal.Priority = command.Request.Priority.Value;
        if (command.Request.TargetDate.HasValue)
            goal.TargetDate = DateTime.SpecifyKind(command.Request.TargetDate.Value, DateTimeKind.Utc);
        if (command.Request.Category != null)
            goal.Category = command.Request.Category;
        if (command.Request.IconName != null)
            goal.IconName = command.Request.IconName;
        if (command.Request.Notes != null)
            goal.Notes = command.Request.Notes;
        if (command.Request.IsActive.HasValue)
            goal.IsActive = command.Request.IsActive.Value;

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
            MonthsToAcquire = null
        };
    }
}
