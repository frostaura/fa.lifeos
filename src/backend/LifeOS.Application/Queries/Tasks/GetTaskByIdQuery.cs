using LifeOS.Application.DTOs.Tasks;
using MediatR;

namespace LifeOS.Application.Queries.Tasks;

public record GetTaskByIdQuery(Guid UserId, Guid Id) : IRequest<TaskDetailResponse?>;
