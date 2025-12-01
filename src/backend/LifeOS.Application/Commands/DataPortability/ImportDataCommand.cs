using LifeOS.Application.DTOs.DataPortability;
using MediatR;

namespace LifeOS.Application.Commands.DataPortability;

public record ImportDataCommand(
    Guid UserId,
    LifeOSExportDto Data,
    string Mode = "replace",
    bool DryRun = false
) : IRequest<ImportResultDto>;
