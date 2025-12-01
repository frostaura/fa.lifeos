using LifeOS.Application.DTOs.FxRates;
using MediatR;

namespace LifeOS.Application.Commands.Finances;

public record RefreshFxRatesCommand() : IRequest<FxRateRefreshResponse>;
