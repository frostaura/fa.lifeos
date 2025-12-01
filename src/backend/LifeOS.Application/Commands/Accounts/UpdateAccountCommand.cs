using LifeOS.Domain.Enums;
using MediatR;

namespace LifeOS.Application.Commands.Accounts;

public record UpdateAccountCommand(
    Guid UserId,
    Guid AccountId,
    string? Name,
    AccountType? AccountType,
    string? Currency,
    decimal? CurrentBalance,
    string? Institution,
    bool? IsLiability,
    decimal? InterestRateAnnual,
    CompoundingFrequency? InterestCompounding,
    decimal? MonthlyFee,
    Dictionary<string, object>? Metadata,
    bool? IsActive
) : IRequest<bool>;
