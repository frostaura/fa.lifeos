using LifeOS.Application.DTOs.Simulations;
using MediatR;

namespace LifeOS.Application.Queries.Simulations;

public record GetEventsQuery(
    Guid UserId,
    Guid? ScenarioId
) : IRequest<EventListResponse>;
