using LifeOS.Application.DTOs.Transactions;
using MediatR;

namespace LifeOS.Application.Queries.Transactions;

public record GetTransactionByIdQuery(
    Guid UserId,
    Guid TransactionId
) : IRequest<TransactionDetailResponse?>;
