using Microsoft.EntityFrameworkCore;
using QPS.Application.Common.Interfaces;
using QPS.Data;
using QPS.Domain.Entities;
using QPS.Domain.Enums;

namespace QPS.Infrastructure.Services;

/// <summary>Adds notifications to the current DbContext; the caller's SaveChanges persists them.</summary>
public class NotificationService : INotificationService
{
    private readonly AppDbContext _context;

    public NotificationService(AppDbContext context) => _context = context;

    public void Notify(Guid userId, string message, NotificationType type, string? link = null)
    {
        _context.Notifications.Add(new Notification
        {
            UserId = userId,
            Message = Trim(message),
            Type = type,
            Link = link,
            IsRead = false
        });
    }

    public async Task NotifyAdminsAsync(string message, NotificationType type, string? link = null, CancellationToken ct = default)
    {
        var adminIds = await _context.Users
            .Where(u => u.Role == UserRole.Admin && u.IsActive && !u.IsDeleted)
            .Select(u => u.Id)
            .ToListAsync(ct);
        foreach (var id in adminIds)
            Notify(id, message, type, link);
    }

    private static string Trim(string message) => message.Length <= 500 ? message : message[..497] + "...";
}
