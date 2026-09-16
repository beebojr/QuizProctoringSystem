namespace QPS.Application.Common.Interfaces;

public interface INotificationService
{
    Task NotifyAsync(Guid userId, string message, CancellationToken ct = default);
    Task NotifyManyAsync(List<Guid> userIds, string message, CancellationToken ct = default);
}