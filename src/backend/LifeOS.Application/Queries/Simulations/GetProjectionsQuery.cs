using LifeOS.Application.DTOs.Simulations;
using MediatR;

namespace LifeOS.Application.Queries.Simulations;

public record GetProjectionsQuery(
    Guid UserId,
    Guid ScenarioId,
    DateOnly? From,
    DateOnly? To,
    string Granularity,
    Guid? AccountId
) : IRequest<ProjectionResponse>;
