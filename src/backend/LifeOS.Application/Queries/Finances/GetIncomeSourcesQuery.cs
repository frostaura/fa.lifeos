using LifeOS.Application.DTOs.Finances;
using MediatR;

namespace LifeOS.Application.Queries.Finances;

public record GetIncomeSourcesQuery(
    Guid UserId
) : IRequest<IncomeSourceListResponse>;
