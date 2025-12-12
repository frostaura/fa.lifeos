using LifeOS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LifeOS.Application.Commands.Identity;

public record UpdateIdentityTargetsCommand(
    Guid UserId,
    Dictionary<string, int> Targets
) : IRequest<bool>;

public class UpdateIdentityTargetsCommandHandler : IRequestHandler<UpdateIdentityTargetsCommand, bool>
{
    private readonly ILifeOSDbContext _context;

    public UpdateIdentityTargetsCommandHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UpdateIdentityTargetsCommand request, CancellationToken cancellationToken)
    {
        var profile = await _context.IdentityProfiles
            .FirstOrDefaultAsync(p => p.UserId == request.UserId, cancellationToken);

        if (profile == null)
            return false;

        profile.PrimaryStatTargets = JsonSerializer.Serialize(request.Targets);
        profile.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
