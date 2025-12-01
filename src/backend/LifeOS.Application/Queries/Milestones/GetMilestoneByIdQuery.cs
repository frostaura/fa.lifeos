using LifeOS.Application.DTOs.Milestones;
using MediatR;

namespace LifeOS.Application.Queries.Milestones;

public record GetMilestoneByIdQuery(Guid UserId, Guid Id) : IRequest<MilestoneDetailResponse?>;
