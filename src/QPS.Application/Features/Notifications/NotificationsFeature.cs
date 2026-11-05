using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Common.Exceptions;
using QPS.Application.Common.Interfaces;
using QPS.Application.Common.Models;
using QPS.Domain.Enums;

namespace QPS.Application.Features.Notifications;

public record NotificationDto(Guid Id, string Message, NotificationType Type, string? Link, bool IsRead, DateTime CreatedAt);

public record NotificationFeedDto(int UnreadCount, List<NotificationDto> Items);

/// <summary>The signed-in user's notifications (newest first).</summary>
public record GetNotificationsQuery(int Take = 50, bool UnreadOnly = false) : IRequest<NotificationFeedDto>;

public record MarkAsReadCommand(Guid NotificationId) : IRequest<Result>;

public record MarkAllAsReadCommand : IRequest<Result>;

public class NotificationHandlers :
    IRequestHandler<GetNotificationsQuery, NotificationFeedDto>,
    IRequestHandler<MarkAsReadCommand, Result>,
    IRequestHandler<MarkAllAsReadCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public NotificationHandlers(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<NotificationFeedDto> Handle(GetNotificationsQuery request, CancellationToken ct)
    {
        var userId = _currentUser.RequireUserId();
        var mine = _db.Notifications.AsNoTracking().Where(n => n.UserId == userId);
        var unread = await mine.CountAsync(n => !n.IsRead, ct);
        var query = request.UnreadOnly ? mine.Where(n => !n.IsRead) : mine;
        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(Math.Clamp(request.Take, 1, 200))
            .Select(n => new NotificationDto(n.Id, n.Message, n.Type, n.Link, n.IsRead, n.CreatedAt))
            .ToListAsync(ct);
        return new NotificationFeedDto(unread, items);
    }

    public async Task<Result> Handle(MarkAsReadCommand request, CancellationToken ct)
    {
        var userId = _currentUser.RequireUserId();
        var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == request.NotificationId && x.UserId == userId, ct)
                ?? throw new NotFoundException("Notification", request.NotificationId);
        n.IsRead = true;
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> Handle(MarkAllAsReadCommand request, CancellationToken ct)
    {
        var userId = _currentUser.RequireUserId();
        var unread = await _db.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync(ct);
        foreach (var n in unread) n.IsRead = true;
        await _db.SaveChangesAsync(ct);
        return Result.Ok($"Marked {unread.Count} as read.");
    }
}
