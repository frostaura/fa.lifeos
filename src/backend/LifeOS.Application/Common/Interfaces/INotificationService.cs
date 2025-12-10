namespace LifeOS.Application.Common.Interfaces;

public interface INotificationService
{
    Task NotifyProjectionsUpdatedAsync(Guid userId);
    Task NotifyCalculationProgressAsync(Guid userId, string mainMessage, string subStep);
}
