using LifeOS.Application.DTOs.Simulations;
using MediatR;

namespace LifeOS.Application.Queries.Simulations;

public record GetScenarioByIdQuery(
    Guid UserId,
    Guid ScenarioId
) : IRequest<ScenarioDetailResponse?>;
