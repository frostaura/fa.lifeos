using LifeOS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Commands.Finances;

public class DeleteIncomeSourceCommandHandler : IRequestHandler<DeleteIncomeSourceCommand, bool>
{
    private readonly ILifeOSDbContext _context;

    public DeleteIncomeSourceCommandHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeleteIncomeSourceCommand request, CancellationToken cancellationToken)
    {
        var incomeSource = await _context.IncomeSources
            .FirstOrDefaultAsync(i => i.Id == request.IncomeSourceId && i.UserId == request.UserId, cancellationToken);

        if (incomeSource == null)
            return false;

        _context.IncomeSources.Remove(incomeSource);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
