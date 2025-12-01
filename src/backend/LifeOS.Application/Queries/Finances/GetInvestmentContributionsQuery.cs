using LifeOS.Application.DTOs.Finances;
using MediatR;

namespace LifeOS.Application.Queries.Finances;

public record GetInvestmentContributionsQuery(
    Guid UserId
) : IRequest<InvestmentContributionListResponse>;
