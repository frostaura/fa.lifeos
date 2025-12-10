using LifeOS.Application.DTOs.Dimensions;
using MediatR;

namespace LifeOS.Application.Queries.Dimensions;

public record GetDimensionsQuery(Guid UserId) : IRequest<DimensionListResponse>;
