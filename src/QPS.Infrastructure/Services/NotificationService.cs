using QPS.Application.Common.Interfaces;
using QPS.Data;
using QPS.Domain.Entities;

namespace QPS.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _context;

    public NotificationService(AppDbContext context)
        => _context = context;

    public async Task NotifyAsync(Guid userId, string message, CancellationToken ct = default)
    {
        var notification = new Notification
        {
            UserId = userId,
            Message = message,
            IsRead = false
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync(ct);
    }

    public async Task NotifyManyAsync(List<Guid> userIds, string message, CancellationToken ct = default)
    {
        var notifications = userIds.Select(userId => new Notification
        {
            UserId = userId,
            Message = message,
            IsRead = false
        }).ToList();

        _context.Notifications.AddRange(notifications);
        await _context.SaveChangesAsync(ct);
    }
}
