using LifeOS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Commands.Finances;

public class UpdateIncomeSourceCommandHandler : IRequestHandler<UpdateIncomeSourceCommand, bool>
{
    private readonly ILifeOSDbContext _context;

    public UpdateIncomeSourceCommandHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UpdateIncomeSourceCommand request, CancellationToken cancellationToken)
    {
        var incomeSource = await _context.IncomeSources
            .FirstOrDefaultAsync(i => i.Id == request.IncomeSourceId && i.UserId == request.UserId, cancellationToken);

        if (incomeSource == null)
            return false;

        if (!string.IsNullOrEmpty(request.Name))
            incomeSource.Name = request.Name;

        if (request.BaseAmount.HasValue)
            incomeSource.BaseAmount = request.BaseAmount.Value;

        if (request.ClearTaxProfile)
            incomeSource.TaxProfileId = null;
        else if (request.TaxProfileId.HasValue)
            incomeSource.TaxProfileId = request.TaxProfileId.Value;

        if (request.PaymentFrequency.HasValue)
            incomeSource.PaymentFrequency = request.PaymentFrequency.Value;

        if (request.NextPaymentDate.HasValue)
            incomeSource.NextPaymentDate = request.NextPaymentDate.Value;

        if (request.AnnualIncreaseRate.HasValue)
            incomeSource.AnnualIncreaseRate = request.AnnualIncreaseRate.Value;

        if (request.EmployerName != null)
            incomeSource.EmployerName = request.EmployerName;

        if (request.Notes != null)
            incomeSource.Notes = request.Notes;

        if (request.IsActive.HasValue)
            incomeSource.IsActive = request.IsActive.Value;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
