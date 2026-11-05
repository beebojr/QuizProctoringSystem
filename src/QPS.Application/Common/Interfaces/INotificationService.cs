using QPS.Domain.Enums;

namespace QPS.Application.Common.Interfaces;

/// <summary>
/// Queues in-app notifications on the current unit of work; they are saved together with
/// the change that caused them.
/// </summary>
public interface INotificationService
{
    void Notify(Guid userId, string message, NotificationType type, string? link = null);
    Task NotifyAdminsAsync(string message, NotificationType type, string? link = null, CancellationToken ct = default);
}
