using LifeOS.Application.DTOs.Finances;
using MediatR;

namespace LifeOS.Application.Queries.Finances;

public record GetExpenseDefinitionsQuery(
    Guid UserId
) : IRequest<ExpenseDefinitionListResponse>;
