using LifeOS.Application.DTOs.Transactions;
using LifeOS.Domain.Enums;
using MediatR;

namespace LifeOS.Application.Commands.Transactions;

public record CreateTransactionCommand(
    Guid UserId,
    Guid? SourceAccountId,
    Guid? TargetAccountId,
    string Currency,
    decimal Amount,
    TransactionCategory Category,
    string? Subcategory,
    string[]? Tags,
    string? Description,
    string? Notes,
    DateOnly TransactionDate
) : IRequest<TransactionDetailResponse>;
