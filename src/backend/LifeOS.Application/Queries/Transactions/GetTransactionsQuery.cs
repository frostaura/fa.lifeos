using LifeOS.Application.DTOs.Milestones;
using LifeOS.Application.DTOs.Transactions;
using MediatR;

namespace LifeOS.Application.Queries.Transactions;

public record GetTransactionsQuery(
    Guid UserId,
    Guid? AccountId = null,
    string? Category = null,
    DateOnly? From = null,
    DateOnly? To = null,
    decimal? MinAmount = null,
    decimal? MaxAmount = null,
    string[]? Tags = null,
    bool? IsReconciled = null,
    string? Sort = null,
    int Page = 1,
    int PerPage = 20
) : IRequest<TransactionListResponse>;
