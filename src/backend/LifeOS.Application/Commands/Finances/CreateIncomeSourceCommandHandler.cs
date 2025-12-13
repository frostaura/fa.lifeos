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
            IsActive = true,
            TargetAccountId = request.TargetAccountId,
            EndConditionType = request.EndConditionType,
            EndConditionAccountId = request.EndConditionAccountId,
            EndDate = request.EndDate,
            EndAmountThreshold = request.EndAmountThreshold
        };

        _context.IncomeSources.Add(incomeSource);
        await _context.SaveChangesAsync(cancellationToken);

        // Get target account name if set
        string? targetAccountName = null;
        if (incomeSource.TargetAccountId.HasValue)
        {
            var account = await _context.Accounts
                .FindAsync(new object[] { incomeSource.TargetAccountId.Value }, cancellationToken);
            targetAccountName = account?.Name;
        }
        
        // Get end condition account name if set
        string? endConditionAccountName = null;
        if (incomeSource.EndConditionAccountId.HasValue)
        {
            var account = await _context.Accounts
                .FindAsync(new object[] { incomeSource.EndConditionAccountId.Value }, cancellationToken);
            endConditionAccountName = account?.Name;
        }

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
                    IsActive = incomeSource.IsActive,
                    TargetAccountId = incomeSource.TargetAccountId,
                    TargetAccountName = targetAccountName,
                    EndConditionType = incomeSource.EndConditionType.ToString().ToLowerInvariant(),
                    EndConditionAccountId = incomeSource.EndConditionAccountId,
                    EndConditionAccountName = endConditionAccountName,
                    EndDate = incomeSource.EndDate,
                    EndAmountThreshold = incomeSource.EndAmountThreshold
                }
            }
        };
    }
}
