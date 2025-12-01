using MediatR;

namespace LifeOS.Application.Commands.Finances;

public record DeleteTaxProfileCommand(
    Guid UserId,
    Guid TaxProfileId
) : IRequest<bool>;
