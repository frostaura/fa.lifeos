using MediatR;

namespace LifeOS.Application.Commands.Finances;

public record DeleteExpenseDefinitionCommand(
    Guid UserId,
    Guid ExpenseDefinitionId
) : IRequest<bool>;
