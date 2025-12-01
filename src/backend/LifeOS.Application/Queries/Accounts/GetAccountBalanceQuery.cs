using LifeOS.Application.DTOs.Accounts;
using MediatR;

namespace LifeOS.Application.Queries.Accounts;

public record GetAccountBalanceQuery(
    Guid UserId,
    Guid AccountId,
    string? TargetCurrency = null
) : IRequest<AccountBalanceResponse?>;
