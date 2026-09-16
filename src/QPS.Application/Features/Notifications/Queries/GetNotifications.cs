using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Common.Interfaces;
using QPS.Application.Features.Notifications.DTOs;

namespace QPS.Application.Features.Notifications.Queries;

public record GetNotificationsQuery(
    Guid? UserId = null,
    bool? IsRead = null) : IRequest<List<NotificationDto>>;

public class GetNotificationsHandler : IRequestHandler<GetNotificationsQuery, List<NotificationDto>>
{
    private readonly IApplicationDbContext _context;

    public GetNotificationsHandler(IApplicationDbContext context) => _context = context;

    public async Task<List<NotificationDto>> Handle(
        GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Notifications
            .AsNoTracking()
            .AsQueryable();

        if (request.UserId.HasValue)
            query = query.Where(n => n.UserId == request.UserId.Value);

        if (request.IsRead.HasValue)
            query = query.Where(n => n.IsRead == request.IsRead.Value);

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto(n.Id, n.Message, n.IsRead, n.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}