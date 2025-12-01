using LifeOS.Application.DTOs.FxRates;
using MediatR;

namespace LifeOS.Application.Queries.FxRates;

public record GetFxRatesQuery(
    Guid UserId
) : IRequest<FxRateListResponse>;
