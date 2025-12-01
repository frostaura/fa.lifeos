using LifeOS.Application.DTOs.Simulations;
using MediatR;

namespace LifeOS.Application.Queries.Simulations;

public record GetEventByIdQuery(
    Guid UserId,
    Guid EventId
) : IRequest<EventDetailResponse?>;
