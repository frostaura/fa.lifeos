using LifeOS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Commands.Metrics;

public record UpdateMetricRecordCommand(
    Guid UserId,
    Guid RecordId,
    decimal? ValueNumber,
    bool? ValueBoolean,
    string? ValueString,
    string? Notes,
    string? Metadata
) : IRequest<bool>;

public class UpdateMetricRecordCommandHandler : IRequestHandler<UpdateMetricRecordCommand, bool>
{
    private readonly ILifeOSDbContext _context;

    public UpdateMetricRecordCommandHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UpdateMetricRecordCommand request, CancellationToken cancellationToken)
    {
        var record = await _context.MetricRecords
            .FirstOrDefaultAsync(r => r.Id == request.RecordId && r.UserId == request.UserId, cancellationToken);

        if (record == null)
            return false;

        if (request.ValueNumber.HasValue)
            record.ValueNumber = request.ValueNumber.Value;

        if (request.ValueBoolean.HasValue)
            record.ValueBoolean = request.ValueBoolean.Value;

        if (request.ValueString != null)
            record.ValueString = request.ValueString;

        if (request.Notes != null)
            record.Notes = request.Notes;

        if (request.Metadata != null)
            record.Metadata = request.Metadata;

        record.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
