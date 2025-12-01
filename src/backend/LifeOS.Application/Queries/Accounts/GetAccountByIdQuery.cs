using LifeOS.Application.DTOs.Accounts;
using MediatR;

namespace LifeOS.Application.Queries.Accounts;

public record GetAccountByIdQuery(
    Guid UserId,
    Guid AccountId
) : IRequest<AccountDetailResponse?>;
