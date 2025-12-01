using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Finances;
using LifeOS.Domain.Entities;
using MediatR;

namespace LifeOS.Application.Commands.Finances;

public class CreateIncomeSourceCommandHandler : IRequestHandler<CreateIncomeSourceCommand, IncomeSourceDetailResponse>
{
    private readonly ILifeOSDbContext _context;

    public CreateIncomeSourceCommandHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<IncomeSourceDetailResponse> Handle(CreateIncomeSourceCommand request, CancellationToken cancellationToken)
    {
        var incomeSource = new IncomeSource
        {
            UserId = request.UserId,
            Name = request.Name,
            Currency = request.Currency.ToUpperInvariant(),
            BaseAmount = request.BaseAmount,
            IsPreTax = request.IsPreTax,
            TaxProfileId = request.TaxProfileId,
            PaymentFrequency = request.PaymentFrequency,
            NextPaymentDate = request.NextPaymentDate,
            AnnualIncreaseRate = request.AnnualIncreaseRate,
            EmployerName = request.EmployerName,
            Notes = request.Notes,
            IsActive = true
        };

        _context.IncomeSources.Add(incomeSource);
        await _context.SaveChangesAsync(cancellationToken);

        return new IncomeSourceDetailResponse
        {
            Data = new IncomeSourceItemResponse
            {
                Id = incomeSource.Id,
                Type = "incomeSource",
                Attributes = new IncomeSourceAttributes
                {
                    Name = incomeSource.Name,
                    Currency = incomeSource.Currency,
                    BaseAmount = incomeSource.BaseAmount,
                    IsPreTax = incomeSource.IsPreTax,
                    TaxProfileId = incomeSource.TaxProfileId,
                    PaymentFrequency = incomeSource.PaymentFrequency.ToString().ToLowerInvariant(),
                    NextPaymentDate = incomeSource.NextPaymentDate,
                    AnnualIncreaseRate = incomeSource.AnnualIncreaseRate,
                    EmployerName = incomeSource.EmployerName,
                    Notes = incomeSource.Notes,
                    IsActive = incomeSource.IsActive
                }
            }
        };
    }
}
