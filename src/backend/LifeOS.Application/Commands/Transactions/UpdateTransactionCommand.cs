using MediatR;

namespace LifeOS.Application.Commands.Transactions;

public record UpdateTransactionCommand(
    Guid UserId,
    Guid TransactionId,
    string? Subcategory,
    string[]? Tags,
    string? Description,
    string? Notes,
    bool? IsReconciled
) : IRequest<bool>;
