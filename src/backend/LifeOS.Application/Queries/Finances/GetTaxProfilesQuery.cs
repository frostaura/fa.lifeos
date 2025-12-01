using LifeOS.Application.DTOs.Finances;
using MediatR;

namespace LifeOS.Application.Queries.Finances;

public record GetTaxProfilesQuery(
    Guid UserId
) : IRequest<TaxProfileListResponse>;
