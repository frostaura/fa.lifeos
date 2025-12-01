using MediatR;

namespace LifeOS.Application.Commands.Transactions;

public record DeleteTransactionCommand(
    Guid UserId,
    Guid TransactionId
) : IRequest<bool>;
