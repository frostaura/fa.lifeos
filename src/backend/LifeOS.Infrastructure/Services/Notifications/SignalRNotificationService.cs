using LifeOS.Infrastructure.Hubs;
using LifeOS.Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace LifeOS.Infrastructure.Services.Notifications;

public class SignalRNotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public SignalRNotificationService(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyProjectionsUpdatedAsync(Guid userId)
    {
        await _hubContext.Clients
            .User(userId.ToString())
            .SendAsync("ProjectionsUpdated");
    }

    public async Task NotifyCalculationProgressAsync(Guid userId, string mainMessage, string subStep)
    {
        await _hubContext.Clients
            .User(userId.ToString())
            .SendAsync("CalculationProgress", new { mainMessage, subStep });
    }
}
