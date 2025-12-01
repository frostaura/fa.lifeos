using LifeOS.Application.DTOs.Accounts;
using MediatR;

namespace LifeOS.Application.Queries.Accounts;

public record GetAccountsQuery(
    Guid UserId,
    string? AccountType = null,
    string? Currency = null,
    bool? IsActive = null,
    bool? IsLiability = null,
    int Page = 1,
    int PerPage = 20
) : IRequest<AccountListResponse>;
