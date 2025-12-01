using LifeOS.Application.DTOs.Simulations;
using MediatR;

namespace LifeOS.Application.Commands.Simulations;

public record CreateScenarioCommand(
    Guid UserId,
    string Name,
    string? Description,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string? EndCondition,
    Dictionary<string, object>? BaseAssumptions,
    bool IsBaseline
) : IRequest<ScenarioDetailResponse>;
