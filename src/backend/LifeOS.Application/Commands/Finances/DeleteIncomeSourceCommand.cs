using MediatR;

namespace LifeOS.Application.Commands.Finances;

public record DeleteIncomeSourceCommand(
    Guid UserId,
    Guid IncomeSourceId
) : IRequest<bool>;
