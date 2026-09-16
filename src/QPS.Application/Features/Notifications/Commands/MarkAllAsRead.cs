using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Common.Interfaces;
using QPS.Application.Common.Models;

namespace QPS.Application.Features.Notifications.Commands;

public record MarkAllAsReadCommand : IRequest<Result>;

public class MarkAllAsReadHandler : IRequestHandler<MarkAllAsReadCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public MarkAllAsReadHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(MarkAllAsReadCommand request, CancellationToken cancellationToken)
    {
        var unread = await _context.Notifications
            .Where(n => !n.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var notification in unread)
            notification.IsRead = true;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Ok($"Marked {unread.Count} notifications as read");
    }
}