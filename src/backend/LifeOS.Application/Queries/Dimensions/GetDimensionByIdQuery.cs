using LifeOS.Application.DTOs.Dimensions;
using MediatR;

namespace LifeOS.Application.Queries.Dimensions;

public record GetDimensionByIdQuery(Guid UserId, Guid Id) : IRequest<DimensionDetailResponse?>;
