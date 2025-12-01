using MediatR;

namespace LifeOS.Application.Commands.Dimensions;

public record UpdateDimensionWeightCommand(Guid Id, decimal Weight, bool AutoRebalance = true) : IRequest<bool>;
