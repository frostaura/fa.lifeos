using LifeOS.Application.DTOs.Milestones;
using MediatR;

namespace LifeOS.Application.Queries.Milestones;

public record GetMilestonesQuery(
    Guid UserId,
    Guid? DimensionId = null,
    string? Status = null,
    string? Sort = null,
    int Page = 1,
    int PerPage = 20
) : IRequest<MilestoneListResponse>;
