using LifeOS.Application.DTOs.Accounts;
using LifeOS.Domain.Enums;
using MediatR;

namespace LifeOS.Application.Commands.Accounts;

public record CreateAccountCommand(
    Guid UserId,
    string Name,
    AccountType AccountType,
    string Currency,
    decimal InitialBalance,
    string? Institution,
    bool IsLiability,
    decimal? InterestRateAnnual,
    CompoundingFrequency? InterestCompounding,
    decimal MonthlyFee,
    Dictionary<string, object>? Metadata
) : IRequest<AccountDetailResponse>;
