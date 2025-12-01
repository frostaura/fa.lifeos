using MediatR;

namespace LifeOS.Application.Commands.Simulations;

public record UpdateScenarioCommand(
    Guid UserId,
    Guid ScenarioId,
    string? Name,
    string? Description,
    DateOnly? EndDate,
    string? EndCondition,
    Dictionary<string, object>? BaseAssumptions,
    bool? IsBaseline
) : IRequest<bool>;
