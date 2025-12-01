using LifeOS.Application.DTOs.Finances;
using MediatR;

namespace LifeOS.Application.Queries.Finances;

public record GetFinancialGoalsQuery(
    Guid UserId
) : IRequest<FinancialGoalListResponse>;
