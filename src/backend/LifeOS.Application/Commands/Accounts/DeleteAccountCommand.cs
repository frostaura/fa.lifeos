using MediatR;

namespace LifeOS.Application.Commands.Accounts;

public record DeleteAccountCommand(
    Guid UserId,
    Guid AccountId
) : IRequest<bool>;
