using LifeOS.Application.Common.Interfaces;
using LifeOS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LifeOS.Application.Commands.Identity;

public record UpdateIdentityProfileCommand(
    Guid UserId,
    string Archetype,
    string? ArchetypeDescription,
    List<string> Values,
    Dictionary<string, int> PrimaryStatTargets,
    List<Guid> LinkedMilestoneIds
) : IRequest<bool>;

public class UpdateIdentityProfileCommandHandler : IRequestHandler<UpdateIdentityProfileCommand, bool>
{
    private readonly ILifeOSDbContext _context;

    public UpdateIdentityProfileCommandHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UpdateIdentityProfileCommand request, CancellationToken cancellationToken)
    {
        var profile = await _context.IdentityProfiles
            .FirstOrDefaultAsync(p => p.UserId == request.UserId, cancellationToken);

        if (profile == null)
        {
            // Create new profile
            profile = new IdentityProfile
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                CreatedAt = DateTime.UtcNow
            };
            _context.IdentityProfiles.Add(profile);
        }

        profile.Archetype = request.Archetype;
        profile.ArchetypeDescription = request.ArchetypeDescription;
        profile.Values = JsonSerializer.Serialize(request.Values);
        profile.PrimaryStatTargets = JsonSerializer.Serialize(request.PrimaryStatTargets);
        profile.LinkedMilestoneIds = JsonSerializer.Serialize(request.LinkedMilestoneIds);
        profile.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
