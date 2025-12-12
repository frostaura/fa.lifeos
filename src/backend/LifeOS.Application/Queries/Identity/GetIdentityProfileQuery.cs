using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LifeOS.Application.Queries.Identity;

public record GetIdentityProfileQuery(Guid UserId) : IRequest<IdentityProfileResponse?>;

public class GetIdentityProfileQueryHandler : IRequestHandler<GetIdentityProfileQuery, IdentityProfileResponse?>
{
    private readonly ILifeOSDbContext _context;

    public GetIdentityProfileQueryHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<IdentityProfileResponse?> Handle(GetIdentityProfileQuery request, CancellationToken cancellationToken)
    {
        var profile = await _context.IdentityProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == request.UserId, cancellationToken);

        if (profile == null)
            return null;

        var values = JsonSerializer.Deserialize<List<string>>(profile.Values) ?? new();
        var primaryStatTargets = JsonSerializer.Deserialize<Dictionary<string, int>>(profile.PrimaryStatTargets) ?? new();
        var linkedMilestoneIds = JsonSerializer.Deserialize<List<Guid>>(profile.LinkedMilestoneIds) ?? new();

        // Fetch linked milestones
        var linkedMilestones = await _context.Milestones
            .AsNoTracking()
            .Where(m => linkedMilestoneIds.Contains(m.Id) && m.UserId == request.UserId)
            .Select(m => new LinkedMilestoneDto
            {
                Id = m.Id,
                Title = m.Title
            })
            .ToListAsync(cancellationToken);

        return new IdentityProfileResponse
        {
            Data = new IdentityProfileDataWrapper
            {
                Archetype = profile.Archetype,
                ArchetypeDescription = profile.ArchetypeDescription,
                Values = values,
                PrimaryStatTargets = primaryStatTargets,
                LinkedMilestones = linkedMilestones
            }
        };
    }
}
