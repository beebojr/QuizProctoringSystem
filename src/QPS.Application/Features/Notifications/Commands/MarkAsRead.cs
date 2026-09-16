using MediatR;
using QPS.Application.Common.Interfaces;
using QPS.Application.Common.Models;

namespace QPS.Application.Features.Notifications.Commands;

public record MarkAsReadCommand(Guid NotificationId) : IRequest<Result>;

public class MarkAsReadHandler : IRequestHandler<MarkAsReadCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public MarkAsReadHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(MarkAsReadCommand request, CancellationToken cancellationToken)
    {
        var notification = _context.Notifications
            .FirstOrDefault(n => n.Id == request.NotificationId);

        if (notification == null)
            return Result.Fail("Notification not found");

        notification.IsRead = true;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Ok("Marked as read");
    }
}