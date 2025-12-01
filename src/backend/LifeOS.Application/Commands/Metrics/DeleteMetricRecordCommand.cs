using LifeOS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Commands.Metrics;

public record DeleteMetricRecordCommand(Guid UserId, Guid RecordId) : IRequest<bool>;

public class DeleteMetricRecordCommandHandler : IRequestHandler<DeleteMetricRecordCommand, bool>
{
    private readonly ILifeOSDbContext _context;

    public DeleteMetricRecordCommandHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeleteMetricRecordCommand request, CancellationToken cancellationToken)
    {
        var record = await _context.MetricRecords
            .FirstOrDefaultAsync(r => r.Id == request.RecordId && r.UserId == request.UserId, cancellationToken);

        if (record == null)
            return false;

        _context.MetricRecords.Remove(record);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
