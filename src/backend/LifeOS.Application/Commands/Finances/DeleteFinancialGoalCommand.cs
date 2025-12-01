using LifeOS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Commands.Finances;

public record DeleteFinancialGoalCommand(
    Guid UserId,
    Guid GoalId
) : IRequest<bool>;

public class DeleteFinancialGoalCommandHandler 
    : IRequestHandler<DeleteFinancialGoalCommand, bool>
{
    private readonly ILifeOSDbContext _db;

    public DeleteFinancialGoalCommandHandler(ILifeOSDbContext db)
    {
        _db = db;
    }

    public async Task<bool> Handle(
        DeleteFinancialGoalCommand command,
        CancellationToken cancellationToken)
    {
        var goal = await _db.FinancialGoals
            .FirstOrDefaultAsync(g => g.Id == command.GoalId && g.UserId == command.UserId, cancellationToken);

        if (goal == null)
            return false;

        _db.FinancialGoals.Remove(goal);
        await _db.SaveChangesAsync(cancellationToken);

        return true;
    }
}
