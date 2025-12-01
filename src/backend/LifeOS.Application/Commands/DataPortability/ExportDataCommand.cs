using LifeOS.Application.DTOs.DataPortability;
using MediatR;

namespace LifeOS.Application.Commands.DataPortability;

public record ExportDataCommand(Guid UserId) : IRequest<LifeOSExportDto>;
